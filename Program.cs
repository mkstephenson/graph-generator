using System.Numerics;
using SkiaSharp;

const int mapBuffer = 2;
const int mapWidth = 512 + (mapBuffer * 2);
const int mapHeight = 512 + (mapBuffer * 2);
const float radiusAroundInitialPointForPossibleCoord = 0.6f;
const float radiusAroundPositionForConnection = 5f;
const float scale = 20.0f;
const int mapNodeClusterSize = 64;

List<Node>[,] mapNodeClusters = new List<Node>[mapNodeClusterSize, mapNodeClusterSize];
for (int i = 0; i < mapNodeClusterSize; i++)
{
  for (int j = 0; j < mapNodeClusterSize; j++)
  {
    mapNodeClusters[i, j] = [];
  }
}

for (int x = mapBuffer; x <= mapWidth - mapBuffer; x++)
{
  for (int y = mapBuffer; y <= mapHeight - mapBuffer; y++)
  {
    float xModifier = GetNextRandomPositionModifier();
    float yModifier = GetNextRandomPositionModifier();
    var newPos = new Vector2(xModifier + x, yModifier + y) * scale;
    var newNode = new Node(Guid.NewGuid(), newPos, []);
    int firstDimension = (int)newPos.X / (int)(mapWidth * scale / (mapNodeClusterSize - 1));
    int secondDimension = (int)newPos.Y / (int)(mapHeight * scale / (mapNodeClusterSize - 1));
    mapNodeClusters[firstDimension, secondDimension].Add(newNode);
    Console.WriteLine($"Generated node at {x}:{y} ({newPos * scale})");
  }
}

Parallel.For(0, mapNodeClusterSize, i =>
{
  for (int j = 0; j < mapNodeClusterSize; j++)
  {
    List<Node> relevantNodes = [.. mapNodeClusters[i, j]];

    AddRelevantNodeClusters(mapNodeClusterSize, mapNodeClusters, i, j, relevantNodes);

    foreach (var node in mapNodeClusters[i, j])
    {
      List<Node> neighboringNodes = [];
      float radius = 0;
      do
      {
        radius += radiusAroundPositionForConnection;
        neighboringNodes = [.. relevantNodes.Where(n => n != node
                                               && !n.Nodes.Contains(node)
                                               && Vector2.Distance(n.Position, node.Position) < radius)];
      } while (neighboringNodes.Count < 2);

      node.Nodes.AddRange(neighboringNodes);
    }

    Console.WriteLine($"Processed cluster {i}:{j}");
  }
});

var bounds = new SKRect(0, 0, mapWidth * scale, mapHeight * scale);
using var svgStream = File.Create("map.svg");
using var canvas = SKSvgCanvas.Create(bounds, svgStream);

for (int i = 0; i < mapNodeClusterSize; i++)
{
  for (int j = 0; j < mapNodeClusterSize; j++)
  {
    foreach (var node in mapNodeClusters[i, j])
    {
      foreach (var connected in node.Nodes)
      {
        float x1 = node.Position.X;
        float y1 = node.Position.Y;
        float x2 = connected.Position.X;
        float y2 = connected.Position.Y;
        canvas.DrawLine(x1, y1, x2, y2, new SKPaint { Color = SKColors.Blue, StrokeWidth = 1 });
      }

      float x = node.Position.X;
      float y = node.Position.Y;
      canvas.DrawCircle(x, y, 3, new SKPaint { Color = SKColors.Black });
    }
  }
}

static float GetNextRandomPositionModifier()
{
  int sign = Random.Shared.GetItems([-1, 1], 1)[0];
  float modifier = Random.Shared.NextSingle() * radiusAroundInitialPointForPossibleCoord;
  return modifier * sign;
}

static void AddRelevantNodeClusters(int mapNodeClusterSize, List<Node>[,] mapNodeClusters, int i, int j, List<Node> relevantNodes)
{
  // Left side of cluster
  if (i - 1 >= 0)
  {
    // Top left
    if (j - 1 >= 0)
    {
      relevantNodes.AddRange(mapNodeClusters[i - 1, j - 1]);
    }

    // Mid left
    relevantNodes.AddRange(mapNodeClusters[i - 1, j]);

    // Bottom left
    if (j + 1 < mapNodeClusterSize)
    {
      relevantNodes.AddRange(mapNodeClusters[i - 1, j + 1]);
    }
  }

  // Top mid
  if (j - 1 >= 0)
  {
    relevantNodes.AddRange(mapNodeClusters[i, j - 1]);
  }

  // Bottom mid
  if (j + 1 < mapNodeClusterSize)
  {
    relevantNodes.AddRange(mapNodeClusters[i, j + 1]);
  }

  // Right side of cluster
  if (i + 1 < mapNodeClusterSize)
  {
    // Top right
    if (j - 1 >= 0)
    {
      relevantNodes.AddRange(mapNodeClusters[i + 1, j - 1]);
    }

    // Mid right
    relevantNodes.AddRange(mapNodeClusters[i + 1, j]);

    // Bottom right
    if (j + 1 < mapNodeClusterSize)
    {
      relevantNodes.AddRange(mapNodeClusters[i + 1, j + 1]);
    }
  }
}

record class Node(Guid Id, Vector2 Position, List<Node> Nodes);
