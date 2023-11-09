using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// A state representing one level of the search.
    /// Records pruned tile values for cells and supports undoing this pruning later.
    /// </summary>
    public class StateChange
    {
        // A dictionary that records the pruned tile values for cells.
        Dictionary<Cell, HashSet<Tile>> domainChanges = new Dictionary<Cell, HashSet<Tile>>();

        /// <summary>
        /// Adds a domain change to the state change.
        /// </summary>
        /// <param name="cell">The cell of which the domain was changed.</param>
        /// <param name="tile">The tile which was removed from the domain of the cell.</param>
        /// <returns>True if the change was recorded or false if this was already the case.</returns>
        public bool addDomainChange(Cell cell, Tile tile)
        {
            // Create a new entry in the dictionary if this is the domain change for this cell in this state.
            if (!domainChanges.ContainsKey(cell))
            {
                domainChanges.Add(cell, new HashSet<Tile>());
            }
            Debug.Log("Removed " + tile + " from domain of " + cell);
            return domainChanges[cell].Add(tile);
        }

        /// <summary>
        /// Reverts a state change by restoring all the pruned tile values for each cell.
        /// </summary>
        public void revert()
        {
            foreach (KeyValuePair<Cell, HashSet<Tile>> entry in domainChanges)
            {
                foreach (Tile removedTile in entry.Value)
                {
                    entry.Key.RestoreDomain(removedTile);
                }
            }
        }
    }
}