using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    public class StateChange
    {
        Dictionary<Cell, HashSet<Tile>> domainChanges = new Dictionary<Cell, HashSet<Tile>>();

        public bool addDomainChange(Cell cell, Tile tile)
        {
            if (!domainChanges.ContainsKey(cell))
            {
                domainChanges.Add(cell, new HashSet<Tile>());
            }
            Debug.Log("Removed " + tile + " from domain of " + cell);
            return domainChanges[cell].Add(tile);
        }

        public void revert()
        {
            foreach (KeyValuePair<Cell, HashSet<Tile>> entry in domainChanges)
            {
                foreach (Tile removedTile in entry.Value)
                {
                    entry.Key.restoreDomain(removedTile);
                }
            }
        }
    }
}