using Microsoft.AspNetCore.Mvc;
using P2PDirectoryService.Data;
using System.Net.Sockets;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.MapPost("/CreateNetwork", ([FromBody] NodeDTO node) =>
{
    var networkId = Guid.NewGuid();
    var nodeRec = new NodeDTO
    {
        Id = Guid.NewGuid(),
        IP = node.IP,
        Port = node.Port,
        NodeName = node.NodeName,
        NetworkId = networkId
    };

    var network = new Network
    {
        Id = networkId,
        Nodes = new List<NodeDTO> { nodeRec }
    };
    DataStore.Networks.Add(network);
    return Results.Ok(network.Id);
});
app.MapGet("/GetNetworks", () =>
{
    var networks = DataStore.Networks.Select(network => new NetworkInfo { Id = network.Id, NodeCount = network.Nodes.Count}).ToArray();
    return Results.Ok(networks);
});

app.MapPost("/AddNode/{networkId}", (Guid networkId, [FromBody] NodeDTO node) =>
{
    if(!DataStore.Networks.Any(x => x.Id == networkId))
    {
        return Results.BadRequest("Network not found");
    }
    DataStore.Networks.First(network => network.Id == networkId).Nodes.Add(node);
    return Results.Ok();
});

app.MapPost("/RemoveNode/{networkId}", (Guid networkId, [FromBody] NodeDTO node) =>
{
    if (!DataStore.Networks.Any(x => x.Id == networkId))
    {
        return Results.BadRequest("Network not found");
    }

    DataStore.Networks.First(network => network.Id == networkId).Nodes.Remove(node);

    if(DataStore.Networks.First(network => network.Id == networkId).Nodes.Count == 0)
    {
        DataStore.Networks.Remove(DataStore.Networks.First(network => network.Id == networkId));
    }
    return Results.Ok();
});

app.MapGet("/GetNodes/{networkId}", (Guid networkId) =>
{
    if (!DataStore.Networks.Any(x => x.Id == networkId))
    {
        return Results.BadRequest("Network not found");
    }

    var nodes = DataStore.Networks.First(network => network.Id == networkId).Nodes.ToArray();
    return Results.Ok(nodes);
});

app.MapPost("/DeleteNetwork/{networkId}", (Guid networkId) =>
{
    if (!DataStore.Networks.Any(x => x.Id == networkId))
    {
        return Results.BadRequest("Network not found");
    }

    DataStore.Networks.Remove(DataStore.Networks.First(network => network.Id == networkId));
    return Results.Ok();
});

app.MapPost("AddItem/{networkId}", async (Guid networkId, [FromBody] SendableItem item) =>
{
    using var client = new UdpClient();
    var network = DataStore.Networks.First(network => network.Id == networkId);
    var dataItemBytes = ByteExtensions.GetByteArray(item);

    var tasks = new List<Task>();
    network.Nodes.ForEach(x => tasks.Add(client.SendAsync(dataItemBytes, dataItemBytes.Length, x.IP, x.Port)));
    await Task.WhenAll(tasks);

    return Results.Ok();
});

app.Run();

internal record NetworkInfo
{
    public Guid Id {get; init;}
    public int NodeCount { get; init; }
}

public record Network
{
    public Guid Id { get; init; }
    public List<NodeDTO> Nodes { get; init; }
}

public record NodeDTO
{
    public Guid Id { get; init; }
    public string IP { get; init; }
    public int Port { get; init; }
    public string NodeName { get; init; }
    public Guid NetworkId { get; init; }
}

internal class SendableItem
{
    public Guid Id { get; set; }
    public Guid? SenderId = Guid.Empty;
    public DateTime? Timestamp { get; set; }
    public object? Item { get; set; }
    public bool ExternalItem = true;
}
