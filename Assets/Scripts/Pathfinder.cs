﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pathfinder : MonoBehaviour
{
    Node _startNode;
    Node _goalNode;
    Graph _graph;
    GraphView _graphView;

    PriorityQueue<Node> _frontierNodes;
    List<Node> _exploredNodes;
    List<Node> _pathNodes;

    public Color startColor = Color.green;
    public Color goalColor = Color.red;
    public Color frontierColor = Color.magenta;
    public Color exploredColor = Color.gray;
    public Color pathColor = Color.cyan;
    public Color arrowColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    public bool showIterations = true;
    public bool showColors = true;
    public bool showArrows = true;
    public bool exitOnGoal = true;

    public bool isComplete = false;
    int _iterations = 0;

    public enum Mode
    {
        BreadthFirstSearch = 0,
        Dijkstra = 1,
        GreedyBestFirst = 2,
        AStar = 3
    }

    public Mode mode = Mode.BreadthFirstSearch;

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

        _frontierNodes = new PriorityQueue<Node>();
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
        _startNode.distanceTravelled = 0;
    }

    void ShowColors(GraphView graphView, Node start, Node goal, bool lerpColor = false, float lerpValue = 0.5f)
    {
        if (graphView == null || start == null || goal == null)
        {
            return;
        }

        if (_frontierNodes != null)
        {
            graphView.ColorNodes(_frontierNodes.ToList(), frontierColor, lerpColor, lerpValue);
        }

        if (_exploredNodes != null)
        {
            graphView.ColorNodes(_exploredNodes, exploredColor, lerpColor, lerpValue);
        }

        if (_pathNodes != null && _pathNodes.Count > 0)
        {
            graphView.ColorNodes(_pathNodes, pathColor, lerpColor, lerpValue * 2f);
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

    void ShowColors(bool lerpColor = false, float lerpValue = 0.5f)
    {
        ShowColors(_graphView, _startNode, _goalNode, lerpColor, lerpValue);
    }

    public IEnumerator SearchRoutine(float timeStep = 0.1f)
    {
        float timeStart = Time.realtimeSinceStartup;

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

                if (mode == Mode.BreadthFirstSearch)
                {
                    ExpandFrontierBreadthFirst(currentNode);
                }
                else if (mode == Mode.Dijkstra)
                {
                    ExpandFrontierDijkstra(currentNode);
                }
                else if (mode == Mode.GreedyBestFirst)
                {
                    ExpandFrontierGreedyBestFirst(currentNode);
                }
                else
                {
                    ExpandFrontierAStar(currentNode);
                }


                if (_frontierNodes.Contains(_goalNode))
                {
                    _pathNodes = GetPathNodes(_goalNode);

                    if (exitOnGoal)
                    {
                        isComplete = true;
                        Debug.Log("PATHFINDER mode: " + mode.ToString() + " path length = " + _goalNode.distanceTravelled.ToString());
                    }
                }

                if (showIterations)
                {
                    ShowDiagnostics(true, 0.5f);
                    yield return new WaitForSeconds(timeStep);
                }
            }
            else
            {
                isComplete = true;
            }
        }

        ShowDiagnostics(true, 0.5f);
        Debug.Log("PATHFINDER SearchRoutine: elapsed time = " + (Time.realtimeSinceStartup - timeStart).ToString() + " seconds");
    }

    private void ShowDiagnostics(bool lerpColor = false, float lerpValue = 0.5f)
    {
        if (showColors)
        {
            ShowColors(lerpColor, lerpValue);
        }

        if (_graphView && showArrows)
        {
            _graphView.ShowNodeArrows(_frontierNodes.ToList(), arrowColor);

            if (_frontierNodes.Contains(_goalNode))
            {
                _graphView.ShowNodeArrows(_pathNodes, highlightColor);
            }
        }
    }

    void ExpandFrontierBreadthFirst(Node node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.neighbors.Count; i++)
            {
                if (!_exploredNodes.Contains(node.neighbors[i]) && !_frontierNodes.Contains(node.neighbors[i]))
                {
                    float distanceToNeighbor = _graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTravelled = distanceToNeighbor + node.distanceTravelled + (int)node.nodeType;
                    node.neighbors[i].distanceTravelled = newDistanceTravelled;

                    node.neighbors[i].previous = node;
                    node.neighbors[i].priority = _exploredNodes.Count;
                    _frontierNodes.Enqueue(node.neighbors[i]);
                }
            }
        }
    }

    void ExpandFrontierDijkstra(Node node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.neighbors.Count; i++)
            {
                if (!_exploredNodes.Contains(node.neighbors[i]))
                {
                    float distanceToNeighbor = _graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTravelled = distanceToNeighbor + node.distanceTravelled + (int)node.nodeType;

                    if (float.IsPositiveInfinity(node.neighbors[i].distanceTravelled) || newDistanceTravelled < node.neighbors[i].distanceTravelled)
                    {
                        node.neighbors[i].previous = node;
                        node.neighbors[i].distanceTravelled = newDistanceTravelled;
                    }

                    if (!_frontierNodes.Contains(node.neighbors[i]))
                    {
                        node.neighbors[i].priority = node.neighbors[i].distanceTravelled;
                        _frontierNodes.Enqueue(node.neighbors[i]);
                    }
                }
            }
        }
    }

    void ExpandFrontierAStar(Node node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.neighbors.Count; i++)
            {
                if (!_exploredNodes.Contains(node.neighbors[i]))
                {
                    float distanceToNeighbor = _graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTravelled = distanceToNeighbor + node.distanceTravelled + (int)node.nodeType;

                    if (float.IsPositiveInfinity(node.neighbors[i].distanceTravelled) || newDistanceTravelled < node.neighbors[i].distanceTravelled)
                    {
                        node.neighbors[i].previous = node;
                        node.neighbors[i].distanceTravelled = newDistanceTravelled;
                    }

                    if (!_frontierNodes.Contains(node.neighbors[i]) && _graph != null)
                    {
                        float distanceToGoal = _graph.GetNodeDistance(node.neighbors[i], _goalNode);
                        node.neighbors[i].priority = node.neighbors[i].distanceTravelled + distanceToGoal;
                        _frontierNodes.Enqueue(node.neighbors[i]);
                    }
                }
            }
        }
    }

    void ExpandFrontierGreedyBestFirst(Node node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.neighbors.Count; i++)
            {
                if (!_exploredNodes.Contains(node.neighbors[i]) && !_frontierNodes.Contains(node.neighbors[i]))
                {
                    float distanceToNeighbor = _graph.GetNodeDistance(node, node.neighbors[i]);
                    float newDistanceTravelled = distanceToNeighbor + node.distanceTravelled + (int)node.nodeType;
                    node.neighbors[i].distanceTravelled = newDistanceTravelled;

                    node.neighbors[i].previous = node;

                    if (_graph != null)
                    {
                        node.neighbors[i].priority = _graph.GetNodeDistance(node.neighbors[i], _goalNode);
                    }
                    _frontierNodes.Enqueue(node.neighbors[i]);
                }
            }
        }
    }

    List<Node> GetPathNodes(Node endNode)
    {
        List<Node> path = new List<Node>();

        if (endNode == null)
        {
            return path;
        }

        path.Add(endNode);

        Node currentNode = endNode.previous;

        while (currentNode != null)
        {
            path.Insert(0, currentNode);
            currentNode = currentNode.previous;
        }

        return path;
    }
}
