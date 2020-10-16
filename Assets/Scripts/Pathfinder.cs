using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pathfinder : MonoBehaviour
{
    Node _startNode;
    Node _goalNode;
    Graph _graph;
    GraphView _graphView;

    Queue<Node> _frontierNodes;
    List<Node> _exploredNodes;
    List<Node> _pathNodes;

    public Color startColor = Color.green;
    public Color goalColor = Color.red;
    public Color frontierColor = Color.magenta;
    public Color exploredColor = Color.gray;
    public Color pathColor = Color.cyan;

    public bool isComplete = false;
    int _iterations = 0;

    public void Init(Graph graph, GraphView graphView, Node start, Node goal)
    {
        if (start == null || goal == null || graph == null || graphView == null)
        {
            Debug.LogWarning("PATHFINDER Init error: missing component(s)!");
            return;
        }

        if (start.nodeType == NodeType.Blocked || goal.nodeType == NodeType.Blocked)
        {
            Debug.LogWarning("PATHFINDER Init error: start and goal nodes must be unblocked!");
            return;
        }

        _graph = graph;
        _graphView = graphView;
        _startNode = start;
        _goalNode = goal;

        ShowColors(graphView, start, goal);

        _frontierNodes = new Queue<Node>();
        _frontierNodes.Enqueue(start);

        _exploredNodes = new List<Node>();
        _pathNodes = new List<Node>();

        for (int x = 0; x < graph.Width; x++)
        {
            for (int y = 0; y < graph.Height; y++)
            {
                _graph.nodes[x, y].Reset();
            }
        }

        isComplete = false;
        _iterations = 0;
    }

    void ShowColors(GraphView graphView, Node start, Node goal)
    {
        if (graphView == null || start == null || goal == null)
        {
            return;
        }

        if (_frontierNodes != null)
        {
            graphView.ColorNodes(_frontierNodes.ToList(), frontierColor);
        }

        if (_exploredNodes != null)
        {
            graphView.ColorNodes(_exploredNodes, exploredColor);
        }

        NodeView startNodeView = graphView.nodeViews[start.xIndex, start.yIndex];

        if (startNodeView != null)
        {
            startNodeView.ColorNode(startColor);
        }

        NodeView goalNodeView = graphView.nodeViews[goal.xIndex, goal.yIndex];

        if (goalNodeView != null)
        {
            goalNodeView.ColorNode(goalColor);
        }
    }

    void ShowColors()
    {
        ShowColors(_graphView, _startNode, _goalNode);
    }

    public IEnumerator SearchRoutine(float timeStep = 0.1f)
    {
        yield return null;

        while (!isComplete)
        {
            if (_frontierNodes.Count > 0)
            {
                Node currentNode = _frontierNodes.Dequeue();
                _iterations++;

                if (!_exploredNodes.Contains(currentNode))
                {
                    _exploredNodes.Add(currentNode);
                }

                ExpandFrontier(currentNode);
                ShowColors();

                if (_graphView)
                {
                    _graphView.ShowNodeArrows(_frontierNodes.ToList());
                }

                yield return new WaitForSeconds(timeStep);
            }
            else
            {
                isComplete = true;
            }
        }
    }

    void ExpandFrontier(Node node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.neighbors.Count; i++)
            {
                if (!_exploredNodes.Contains(node.neighbors[i]) && !_frontierNodes.Contains(node.neighbors[i]))
                {
                    node.neighbors[i].previous = node;
                    _frontierNodes.Enqueue(node.neighbors[i]);
                }
            }
        }
    }
}
