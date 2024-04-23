﻿using ControlR.Server.Options;
using ControlR.Shared.Models;
using ControlR.Shared.Services.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ControlR.Server.Services;

public interface IIceServerProvider
{
    Task<IceServer[]> GetIceServers();
}

public class IceServerProvider(
    IOptionsMonitor<ApplicationOptions> _appOptions,
    IMeteredApi _meteredApi,
    ILogger<IceServerProvider> _logger) : IIceServerProvider
{
    public async Task<IceServer[]> GetIceServers()
    {
        try
        {
            if (_appOptions.CurrentValue.UseTwilio &&
                !string.IsNullOrWhiteSpace(_appOptions.CurrentValue.TwilioSid) &&
                !string.IsNullOrWhiteSpace(_appOptions.CurrentValue.TwilioSecret))
            {
                TwilioClient.Init(_appOptions.CurrentValue.TwilioSid, _appOptions.CurrentValue.TwilioSecret);
                var token = TokenResource.Create();
                return token.IceServers
                    .Select(x => new IceServer()
                    {
                        Credential = x.Credential,
                        CredentialType = "password",
                        Urls = x.Urls.ToString(),
                        Username = x.Username
                    })
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting Metered ICE servers.");
        }

        try
        {
            if (_appOptions.CurrentValue.UseMetered &&
                !string.IsNullOrWhiteSpace(_appOptions.CurrentValue.MeteredApiKey))
            {
                return await _meteredApi.GetIceServers(_appOptions.CurrentValue.MeteredApiKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting Metered ICE servers.");
        }

        try
        {
            if (_appOptions.CurrentValue.UseCoTurn &&
                !string.IsNullOrWhiteSpace(_appOptions.CurrentValue.CoTurnSecret))
            {
                // TODO: Get coTURN creds.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getitng coTURN ICE servers.");
        }

        try
        {
            if (_appOptions.CurrentValue.UseStaticIceServers &&
               _appOptions.CurrentValue.IceServers.Count > 0)
            {
                return [.._appOptions.CurrentValue.IceServers];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting ICE servers.");
        }

        _logger.LogWarning("No ICE server provider configured.");
        return [];
    }

    private string GenerateTurnPassword(string secret, string username = "")
    {
        var expiration = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var tempUser = !string.IsNullOrWhiteSpace(username) ? 
            $"{expiration}:{username}" :
            $"{expiration}";

        var key = Encoding.ASCII.GetBytes(secret);
        using var hmacsha1 = new HMACSHA1(key);

        var buffer = Encoding.ASCII.GetBytes(tempUser);
        var hashValue = hmacsha1.ComputeHash(buffer);
        return Convert.ToBase64String(hashValue);
    }
}