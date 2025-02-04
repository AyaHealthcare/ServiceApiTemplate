﻿using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Utility.Extensions;
public static class HttpClientExtensions
{
    /// <summary>
    /// HttpClient extension method. Sends the http request and parses the response in to expected TResponse structure
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="client"></param>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<(HttpResponseMessage, TResponse?)> HttpRequestAndResponseAsync<TRequest, TResponse>(this HttpClient client,
        System.Net.Http.HttpMethod method, string url, TRequest? payload, NameValueCollection? headers = null, bool ensureSuccessStatusCode = true)
    {
        var httpRequest = new HttpRequestMessage(method, url);
        if (payload != null)
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"); //CONTENT-TYPE header

        headers?.AllKeys.ToList().ForEach(key =>
        {
            client.DefaultRequestHeaders.Add(key!, headers[key]);
        });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

        using HttpResponseMessage httpResponse = await client.SendAsync(httpRequest);
        if (ensureSuccessStatusCode)
            httpResponse.EnsureSuccessStatusCode();

        if (typeof(TResponse).GetTypeInfo().IsAssignableFrom(typeof(IHtmlDocument).Ge‌​tTypeInfo()))
        {
            return (httpResponse, (TResponse)await GetDocumentAsync(httpResponse)); //must cast even though we know TResponse is IHtmlDocument
        }

        TResponse? response = default;
        using Stream s = await httpResponse.Content.ReadAsStreamAsync();

        if (typeof(TResponse).IsPrimitive)
        {
            string? val = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                StreamReader reader = new(s);
                val = await reader.ReadToEndAsync();
                response = (TResponse)Convert.ChangeType(val, typeof(TResponse));
            }
            else
                throw new InvalidOperationException($"{httpResponse.StatusCode} - {val}");
        }
        else
        {
            response = await JsonSerializer.DeserializeAsync<TResponse>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        if (response == null) throw new InvalidOperationException("null response");
        return (httpResponse, response);
    }

    /// <summary>
    /// Returns searchable IHtmlDocument
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var document = await BrowsingContext.New().OpenAsync(ResponseFactory, CancellationToken.None);
        return (IHtmlDocument)document;

        void ResponseFactory(VirtualResponse htmlResponse)
        {
            htmlResponse
                .Address(response.RequestMessage!.RequestUri)
                .Status(response.StatusCode);

            MapHeaders(response.Headers);
            MapHeaders(response.Content.Headers);

            htmlResponse.Content(content);

            void MapHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    foreach (var value in header.Value)
                    {
                        htmlResponse.Header(header.Key, value);
                    }
                }
            }
        }
    }
}
