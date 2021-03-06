﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardTiles : MonoBehaviour
{
    Board m_board;

    void Awake()
    {
        m_board = GetComponent<Board>();
    }

    public void BreakTileAt(int x, int y)
    {
        Tile tileToBreak = m_board.allTiles[x, y];

        if (tileToBreak != null && tileToBreak.tileType == TileType.Breakable)
        {
            // Play appropriate particle effect
            ParticleManager.Instance.BreakTileFXAt(tileToBreak.breakableValue, x, y, 0);

            tileToBreak.BreakTile();
        }
    }

    public void BreakTileAt(List<Bubble> bubbles)
    {
        foreach (Bubble bubble in bubbles)
        {
            if (bubble != null)
            {
                BreakTileAt(bubble.xIndex, bubble.yIndex);
            }
        }
    }
}
