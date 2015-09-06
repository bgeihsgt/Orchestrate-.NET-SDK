﻿using System;
using System.Net;
using Orchestrate.Io;
using Xunit;

public class GeoQueriesTests : IClassFixture<TestFixture>, IDisposable
{
    Collection collection;
    string collectionName;

    public GeoQueriesTests(TestFixture testFixture)
    {
        collection = testFixture.Collection;
        collectionName = testFixture.CollectionName;

        GeoCoordinate coordinate = new GeoCoordinate { Latitude = 48.8582M, Longitude = 2.2945M };
        Location location = new Location { Name = "Eiffel Tower", GeoCoordinate = coordinate };
        AsyncHelper.RunSync(() => collection.TryAddAsync("1", location));

        SearchHelper.WaitForConsistency(collection, "value.coordinates:NEAR:{lat:48.8 lon:2.3 dist:100km}", 1);
    }

    public void Dispose()
    {
        AsyncHelper.RunSync(() => collection.DeleteAsync("1"));
    }

    [Fact]
    public async void SearchFindsLocationWithLuceneQuerySyntax()
    {
        string luceneQuery = "value.coordinates:NEAR:{lat:48.8 lon:2.3 dist:100km}";
        var searchResult = await collection.SearchAsync<Location>(luceneQuery);

        Assert.Equal(1, searchResult.Count);
    }

    [Fact]
    public async void SearchFindsLocationWithParameters()
    {
        var searchResult = await collection.SearchAsync<Location>("value.coordinates", 48.8M, 2.3M, "100km");

        Assert.Equal(1, searchResult.Count);
    }

    [Fact]
    public async void SearchDoesNotFindLocationWithLuceneQuerySyntax()
    {
        string luceneQuery = "value.coordinates:NEAR:{lat:48.8 lon:2.3 dist:100m}";
        var searchResult = await collection.SearchAsync<Location>(luceneQuery);

        Assert.Equal(0, searchResult.Count);
    }

    [Fact]
    public async void SearchDoesNotFindLocationWithParameters()
    {
        var searchResult = await collection.SearchAsync<Location>("value.coordinates", 48.8M, 2.3M, "100m");

        Assert.Equal(0, searchResult.Count);
    }

    [Fact]
    public async void InvalidCredentialsThrowsRequestException()
    {
        var application = new Application("HaHa");
        var client = new Client(application);
        var collection = client.GetCollection(collectionName);

        var execption = await Assert.ThrowsAsync<RequestException>(
                                () => collection.SearchAsync<Location>("value.coordinates", 48.8M, 2.3M, "100m"));

        Assert.Equal(HttpStatusCode.Unauthorized, execption.StatusCode);
        Assert.Equal("Valid credentials are required.", execption.Message);
    }
}