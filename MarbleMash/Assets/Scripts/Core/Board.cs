﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A generic GameObject that can be positioned at coordinate (x,y,z) when the game begins
[System.Serializable]
public class StartingObject
{
    public GameObject prefab;
    public int x;
    public int y;
    public int z;
}

[RequireComponent(typeof(BoardBomber))]
[RequireComponent(typeof(BoardClearer))]
[RequireComponent(typeof(BoardCollapser))]
[RequireComponent(typeof(BoardFiller))]
[RequireComponent(typeof(BoardHighlighter))]
[RequireComponent(typeof(BoardInput))]
[RequireComponent(typeof(BoardMatcher))]
[RequireComponent(typeof(BoardQuery))]
[RequireComponent(typeof(BoardSetup))]
[RequireComponent(typeof(BoardTiles))]
public class Board : MonoBehaviour
{
    // Dimensions of Board
    public int width;
    public int height;

    // Margin outside Board for calculating camera field of view
    public int borderSize;

    // Prefab representing a single Tile
    public GameObject tileNormalPrefab;
    // Prefab representing an empty, unoccupied Tile
    public GameObject tileObstaclePrefab;
    // Array of Marble Prefabs
    public GameObject[] marblePrefabs;

    // Prefabs representing Bombs
    public GameObject adjacentBombPrefab;
    public GameObject columnBombPrefab;
    public GameObject rowBombPrefab;

    GameObject m_clickedTileBomb;
    GameObject m_targetTileBomb;

    // The time required to swap Marbles between the target and clicked Tile
    float m_swapTime = 0.5f;
    // The base delay between events
    float m_delay = 0.2f;

    // Array of all the Board's Tiles
    public Tile[,] allTiles;
    // Array of all of the Board's Marbles
    public Marble[,] allMarbles;

    // Tile first clicked by mouse
    public Tile clickedTile;
    // Adjacent Tile dragged into by mouse
    public Tile targetTile;

    // Whether user input is currently allowed
    public bool playerInputEnabled = true;

    // Manually positioned Tiles, placed before the Board is filled
    public StartingObject[] startingTiles;
    // Manually positioned Marbles, placed before the Board is filled
    public StartingObject[] startingMarbles;

    // Y Offset used to make the marbles "fall" into place to fill the Board
    public int fillYOffset = 10;
    // Time used to fill the Board
    public float fillMoveTime = 0.5f;

    // References to Board components
    public BoardBomber boardBomber;
    public BoardClearer boardClearer;
    public BoardCollapser boardCollapser;
    public BoardFiller boardFiller;
    public BoardHighlighter boardHighlighter;
    public BoardInput boardInput;
    public BoardMatcher boardMatcher;
    public BoardQuery boardQuery;
    public BoardSetup boardSetup;
    public BoardTiles boardTiles;

    void Awake()
    {
        boardBomber = GetComponent<BoardBomber>();
        boardClearer = GetComponent<BoardClearer>();
        boardCollapser = GetComponent<BoardCollapser>();
        boardFiller = GetComponent<BoardFiller>();
        boardHighlighter = GetComponent<BoardHighlighter>();
        boardInput = GetComponent<BoardInput>();
        boardMatcher = GetComponent<BoardMatcher>();
        boardQuery = GetComponent<BoardQuery>();
        boardSetup = GetComponent<BoardSetup>();
        boardTiles = GetComponent<BoardTiles>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize array of Tiles
        allTiles = new Tile[width,height];
        // initialize array of Marbles
        allMarbles = new Marble[width,height];

        boardSetup.SetupBoard();
    }

    public void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        // If player input is enabled...
        if (playerInputEnabled)
        {
            // ...set the corresponding Marbles to the clicked Tile and target Tile
            Marble clickedMarble = allMarbles[clickedTile.xIndex, clickedTile.yIndex];
            Marble targetMarble = allMarbles[targetTile.xIndex, targetTile.yIndex];

            if (clickedMarble != null && targetMarble != null)
            {
                // Move the clicked Marble to the target Marble and vice versa
                clickedMarble.Move(targetTile.xIndex, targetTile.yIndex, m_swapTime);
                targetMarble.Move(clickedTile.xIndex, clickedTile.yIndex, m_swapTime);

                // Wait for the swap time
                yield return new WaitForSeconds(m_swapTime);

                // Find all matches for each Marble after the swap
                List<Marble> clickedMarbleMatches = boardMatcher.FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                List<Marble> targetMarbleMatches = boardMatcher.FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

                // If no matches are found, then swap the Marbles back
                if (clickedMarbleMatches.Count == 0 && targetMarbleMatches.Count == 0)
                {
                    clickedMarble.Move(clickedTile.xIndex, clickedTile.yIndex, m_swapTime);
                    targetMarble.Move(targetTile.xIndex, targetTile.yIndex, m_swapTime);

                    yield return new WaitForSeconds(m_swapTime);
                }
                else
                {
                    // Clear matches and refill the Board
                    Vector2 swipeDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex, targetTile.yIndex - clickedTile.yIndex);
                    // Drop bomb in-place
                    m_clickedTileBomb = boardBomber.DropBomb(clickedTile.xIndex, clickedTile.yIndex, swipeDirection, clickedMarbleMatches);
                    m_targetTileBomb = boardBomber.DropBomb(targetTile.xIndex, targetTile.yIndex, swipeDirection, targetMarbleMatches);

                    // Change bomb color to match
                    if (m_clickedTileBomb != null && targetMarble != null)
                    {
                        Bomb clickedBomb = m_clickedTileBomb.GetComponent<Bomb>();
                        clickedBomb.ChangeColor(targetMarble);
                    }

                    if (m_targetTileBomb != null && clickedMarble != null)
                    {
                        Bomb targetBomb = m_targetTileBomb.GetComponent<Bomb>();
                        targetBomb.ChangeColor(clickedMarble);
                    }
                    List<Marble> marblesToClear = clickedMarbleMatches.Union(targetMarbleMatches).ToList();
                    ClearAndRefillBoard(marblesToClear);
                }
            }
        }
    }
    void ClearAndRefillBoard(List<Marble> marbles)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(marbles));
    }

    IEnumerator ClearAndRefillBoardRoutine(List<Marble> marbles)
    {
        // Disable player input while the Board is collapsing/refilling
        playerInputEnabled = false;

        // Create a new List of Marbles, using the initial list as a starting point
        List<Marble> matches = marbles;
        do
        {
            // Run the coroutine to clear the Board and collapse any columns to fill in the spaces
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            // Run the coroutine to refill the Board
            yield return StartCoroutine(boardFiller.RefillRoutine());
            // Find any subsequent matches and repeat the process
            matches = boardMatcher.FindAllMatches();
        }
        while (matches.Count != 0);

        // Re-enable player input
        playerInputEnabled = true;
    }

    IEnumerator ClearAndCollapseRoutine(List<Marble> marbles)
    {
        // List of Marbles to move
        List<Marble> movingMarbles = new List<Marble>();
        // List of Marbles that form matches
        List<Marble> matches = new List<Marble>();

        //HighlightMarbles(marbles);

        bool isFinished = false;
        while (!isFinished)
        {
            // Find Marbles affected by bombs...
            List<Marble> bombedMarbles = boardQuery.GetBombedMarbles(marbles);
            // ...and add to list of Marbles to clear
            marbles = marbles.Union(bombedMarbles).ToList();

            // Clear the Marbles
            boardClearer.ClearMarbleAt(marbles);
            // Break any Tiles under the cleared Marbles
            boardTiles.BreakTileAt(marbles);

            // Activate any generated bombs
            if (m_clickedTileBomb != null)
            {
                boardBomber.ActivateBomb(m_clickedTileBomb);
                m_clickedTileBomb = null;
            }

            if (m_targetTileBomb != null)
            {
                boardBomber.ActivateBomb(m_targetTileBomb);
                m_targetTileBomb = null;
            }

            yield return new WaitForSeconds(m_delay);

            // Collapse any columns with empty spaces and keep track of what Marbles moved as a result
            movingMarbles = boardCollapser.CollapseColumn(marbles);

            // Wait while these Marbles fill in the gaps
            while (!boardQuery.IsCollapsed(movingMarbles))
            {
                yield return null;
            }

            // Find any matches that form from collapsing
            matches = boardMatcher.FindMatchesAt(movingMarbles);

            // If no matches are formed from the collapse, then finish
            if (matches.Count == 0)
            {
                isFinished = true;
            }
            // Otherwise, repeat this process again
            else
            {
                yield return new WaitForSeconds(m_delay);
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }
    }
}
