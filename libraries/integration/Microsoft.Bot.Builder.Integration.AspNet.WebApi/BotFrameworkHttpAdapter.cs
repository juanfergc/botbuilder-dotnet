﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Streaming;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.Integration.AspNet.WebApi
{
    /// <summary>
    /// A Bot Builder Adapter implementation used to handled bot Framework HTTP requests.
    /// </summary>
    public class BotFrameworkHttpAdapter : BotFrameworkHttpAdapterBase, IBotFrameworkHttpAdapter
    {
        private const string AuthHeaderName = "authorization";
        private const string ChannelIdHeaderName = "channelid";

        public BotFrameworkHttpAdapter(ICredentialProvider credentialProvider = null, IChannelProvider channelProvider = null, ILogger<BotFrameworkHttpAdapter> logger = null)
            : base(credentialProvider ?? new SimpleCredentialProvider(), channelProvider, logger)
        {
        }

        public BotFrameworkHttpAdapter(ICredentialProvider credentialProvider, IChannelProvider channelProvider, HttpClient httpClient, ILogger<BotFrameworkHttpAdapter> logger)
            : base(credentialProvider ?? new SimpleCredentialProvider(), channelProvider, httpClient, logger)
        {
        }

        public async Task ProcessAsync(HttpRequestMessage httpRequest, HttpResponseMessage httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            if (httpRequest.Method == HttpMethod.Get)
            {
                await ConnectWebSocketAsync(bot, httpRequest, httpResponse).ConfigureAwait(false);
            }
            else
            {
                // deserialize the incoming Activity
                var activity = await HttpHelper.ReadRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(activity?.Type))
                {
                    httpResponse.StatusCode = HttpStatusCode.BadRequest;
                    return;
                }

                // grab the auth header from the inbound http request
                var authHeader = httpRequest.Headers.Authorization?.ToString();

                try
                {
                    // process the inbound activity with the bot
                    var invokeResponse = await ProcessActivityAsync(authHeader, activity, bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);

                    // write the response, potentially serializing the InvokeResponse
                    HttpHelper.WriteResponse(httpRequest, httpResponse, invokeResponse);
                }
                catch (UnauthorizedAccessException)
                {
                    // handle unauthorized here as this layer creates the http response
                    httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                }
            }
        }

        /// <summary>
        /// Process the initial request to establish a long lived connection via a streaming server.
        /// </summary>
        /// <param name="bot">The <see cref="IBot"/> instance.</param>
        /// <param name="httpRequest">The connection request.</param>
        /// <param name="httpResponse">The response sent on error or connection termination.</param>
        /// <returns>Returns on task completion.</returns>
        private async Task ConnectWebSocketAsync(IBot bot, HttpRequestMessage httpRequest, HttpResponseMessage httpResponse)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            ConnectedBot = bot ?? throw new ArgumentNullException(nameof(bot));

            if (HttpContext.Current.IsWebSocketRequest || HttpContext.Current.IsWebSocketRequestUpgrading)
            {
                httpResponse.StatusCode = HttpStatusCode.BadRequest;
                httpResponse.Content = new StringContent("Upgrade to WebSocket is required.");

                return;
            }

            if (!await AuthenticateRequestAsync(httpRequest, httpResponse).ConfigureAwait(false))
            {
                httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                httpResponse.Content = new StringContent("Request authentication failed.");

                return;
            }

            try
            {
                HttpContext.Current.AcceptWebSocketRequest(async context =>
                {
                    var requestHandler = new StreamingRequestHandler(bot, this, context.WebSocket, Logger);

                    if (RequestHandlers == null)
                    {
                        RequestHandlers = new List<StreamingRequestHandler>();
                    }

                    RequestHandlers.Add(requestHandler);

                    await requestHandler.ListenAsync().ConfigureAwait(false);
                });
            }
            catch (Exception ex)
            {
                httpResponse.StatusCode = HttpStatusCode.InternalServerError;
                httpResponse.Content = new StringContent($"Unable to create transport server. Error: {ex.ToString()}");

                throw ex;
            }
        }

        private async Task<bool> AuthenticateRequestAsync(HttpRequestMessage httpRequest, HttpResponseMessage httpResponse)
        {
            try
            {
                if (!await CredentialProvider.IsAuthenticationDisabledAsync().ConfigureAwait(false))
                {
                    var authHeader = httpRequest.Headers.GetValues(AuthHeaderName.ToLowerInvariant()).FirstOrDefault();
                    var channelId = httpRequest.Headers.GetValues(ChannelIdHeaderName.ToLowerInvariant()).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(authHeader))
                    {
                        WriteUnauthorizedResponse(AuthHeaderName, httpRequest, httpResponse);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(channelId))
                    {
                        WriteUnauthorizedResponse(ChannelIdHeaderName, httpRequest, httpResponse);
                        return false;
                    }

                    var claimsIdentity = await JwtTokenValidation.ValidateAuthHeader(authHeader, CredentialProvider, ChannelProvider, channelId).ConfigureAwait(false);
                    if (!claimsIdentity.IsAuthenticated)
                    {
                        httpResponse.StatusCode = HttpStatusCode.Unauthorized;
                        return false;
                    }
                    
                    // Add ServiceURL to the cache of trusted sites in order to allow token refreshing.
                    AppCredentials.TrustServiceUrl(claimsIdentity.FindFirst(AuthenticationConstants.ServiceUrlClaim).Value);
                    ClaimsIdentity = claimsIdentity;
                }

                return true;
            }
            catch (Exception ex)
            {
                httpResponse.StatusCode = HttpStatusCode.InternalServerError;
                httpResponse.Content = new StringContent("Error while attempting to authorize connection.");

                throw ex;
            }
        }

        private void WriteUnauthorizedResponse(string headerName, HttpRequestMessage httpRequest, HttpResponseMessage httpResponse)
        {
            httpResponse.StatusCode = HttpStatusCode.Unauthorized;
            httpResponse.Content = new StringContent($"Unable to authenticate. Missing header: {headerName}");
        }
    }
}
