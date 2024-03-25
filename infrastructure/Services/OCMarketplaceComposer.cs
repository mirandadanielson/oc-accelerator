﻿using Newtonsoft.Json;
using OC_Accelerator.Models;
using OrderCloud.SDK;
using System.Runtime;
using OC_Accelerator.Helpers;

namespace OC_Accelerator.Services
{
    // TODO: rename this
    public class OCMarketplaceComposer
    {
        private readonly IOrderCloudClient _oc;
        private readonly IAppSettings _appSettings;
        private readonly IgnoreErrorWrapper _ignoreErrorWrapper;

        public OCMarketplaceComposer(IOrderCloudClient oc, IAppSettings appSettings, IgnoreErrorWrapper ignoreErrorWrapper)
        {
            _oc = oc;
            _appSettings = appSettings;
            _ignoreErrorWrapper = ignoreErrorWrapper;
        }

        /// <summary>
        /// Creates API Clients for both the Storefront and Admin applications if either of their respective API ClientIDs are NOT included in appSettings.json
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="storefrontAppName"></param>
        /// <param name="adminAppName"></param>
        /// <returns>a string Tuple representing the Storefront API Client ID (Item1) and the Admin API Client ID (Item2)</returns>
        public async Task<Tuple<string, string>> CreateApiClientsAsync(TextWriter logger, string storefrontAppName, string adminAppName)
        {
            // TODO: temporary
            //if (_appSettings.ocStorefrontClientId == null && _appSettings.ocAdminClientId == null)
            //{
            //    await CleanupAsync(logger, storefrontAppName);
            //}
            
            string storefrontApiClientID = _appSettings.ocStorefrontClientId;
            string adminApiClientID = _appSettings.ocAdminClientId;
            try
            {
                if (storefrontApiClientID == null)
                {
                    await logger.WriteLineAsync($"Creating Buyer {storefrontAppName}");
                    var buyer = await _oc.Buyers.CreateAsync(new Buyer()
                    {
                        ID = string.Join("-", storefrontAppName),
                        Active = true,
                        Name = storefrontAppName
                    });

                    await logger.WriteLineAsync("Creating Default Context User");
                    var defaultContextUser = await _oc.Users.CreateAsync(buyer.ID, new User()
                    {
                        Active = true,
                        Username = "DefaultContextUser",
                        FirstName = "Anonymous",
                        LastName = "User",
                        Email = "test@test.com"
                    });

                    await logger.WriteLineAsync($"Creating Buyer API Client with Default Context User {defaultContextUser.Username}");
                    var storefrontApiClient = new ApiClient
                    {
                        Active = true,
                        AccessTokenDuration = 600,
                        AppName = storefrontAppName,
                        DefaultContextUserName = defaultContextUser.Username,
                        AllowAnyBuyer = true,
                        AllowAnySupplier = false,
                        AllowSeller = false,
                        IsAnonBuyer = false,
                    };
                    storefrontApiClient = await _oc.ApiClients.CreateAsync(storefrontApiClient);
                    storefrontApiClientID = storefrontApiClient.ID;
                }

                if (adminApiClientID == null)
                {
                    await logger.WriteLineAsync("Creating Seller API Client");
                    var adminApiClient = new ApiClient
                    {
                        Active = true,
                        AccessTokenDuration = 600,
                        AppName = adminAppName,
                        DefaultContextUserName = null,
                        AllowAnyBuyer = false,
                        AllowAnySupplier = false,
                        AllowSeller = true,
                        IsAnonBuyer = false,
                    };
                    adminApiClient = await _oc.ApiClients.CreateAsync(adminApiClient);
                    adminApiClientID = adminApiClient.ID;

                }

                return Tuple.Create(storefrontApiClientID, adminApiClientID);
            }
            catch (OrderCloudException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await logger.WriteLineAsync(ex.Message);
                // await CleanupAsync(logger, storefrontAppName);
                throw;
            }
        }

        /// <summary>
        /// Creates the Order Checkout Integration Event in OrderCloud, configured to your Azure functions app
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hostedAppUrl"></param>
        /// <returns></returns>
        public async Task ConfigureOrderCheckoutIntegrationEvent(TextWriter logger, string hostedAppUrl)
        {
            await logger.WriteLineAsync("Creating OrderCheckout Integration Event");
            await _oc.IntegrationEvents.CreateAsync(new IntegrationEvent
            {
                EventType = IntegrationEventType.OrderCheckout,
                CustomImplementationUrl = $"{hostedAppUrl}/api/integrationevent",
                Name = "Order Checkout Integration Event",
                HashKey = _appSettings.ocHashKey
            });
        }

        /// <summary>
        /// Creates a sample Webhook in OrderCloud, configured to your Azure functions app
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hostedAppUrl"></param>
        /// <param name="storefrontApiClientID"></param>
        /// <returns></returns>
        public async Task ConfigureWebhooksAsync(TextWriter logger, string hostedAppUrl, string storefrontApiClientID)
        {
            await logger.WriteLineAsync("Creating Webhook");
            var webhookRoutes = new List<WebhookRoute>()
            {
                new()
                {
                    Route = "v1/buyers/{buyerID}/addresses",
                    Verb = "POST"
                }
            };
            await _oc.Webhooks.CreateAsync(new Webhook
            {
                Name = "Validate Address",
                Description = "Pre-webhook to validate a buyer address before it's created",
                Url = $"{hostedAppUrl}/api/webhook/createaddress",
                HashKey = _appSettings.ocHashKey,
                ElevatedRoles = null,
                ConfigData = null,
                BeforeProcessRequest = true,
                ApiClientIDs = new List<string>() { storefrontApiClientID },
                WebhookRoutes = webhookRoutes,
                DeliveryConfigID = null
            });
        }

        /// <summary>
        /// THIS IS TEMPORARY
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="storefrontAppName"></param>
        /// <returns></returns>
        public async Task CleanupAsync(TextWriter logger, string storefrontAppName)
        {
            var webhooks = await _oc.Webhooks.ListAsync();
            var buyers = await _oc.Buyers.ListAsync(search: storefrontAppName);
            string buyerID = buyers.Items.FirstOrDefault()?.ID;
            foreach (var webhook in webhooks.Items)
            {
                await _oc.Webhooks.DeleteAsync(webhook.ID);
            }

            var integreationEvents = await _oc.IntegrationEvents.ListAsync();
            foreach (var ie in integreationEvents.Items)
            {
                await _oc.IntegrationEvents.DeleteAsync(ie.ID);
            }

            var apiClients = await _oc.ApiClients.ListAsync(filters: new { ID = $"!{_appSettings.ocFunctionsClientId}"});
            foreach (var apiClient in apiClients.Items)
            {
                await _oc.ApiClients.DeleteAsync(apiClient.ID);
            }

            if (buyerID != null)
            {
                await _ignoreErrorWrapper.Ignore404(() => _oc.Buyers.DeleteAsync(buyerID), logger);
                await _ignoreErrorWrapper.Ignore404(() => _oc.Catalogs.DeleteAsync(buyerID), logger);
            }
        }
    }
}
