using System;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Class to represent an arc of two adjacents cells in the MAC3 algorithm.
    /// </summary>
    public class CellArc : IEquatable<CellArc>
    {
        public Cell cell1, cell2; // The adjacent cells in the arc.

        /// <summary>
        /// Constructs an arc between two adjacent cells.
        /// </summary>
        /// <param name="cell1">The first cell of the arc.</param>
        /// <param name="cell2">The second cell of the arc.</param>
        /// <exception cref="ArgumentException">Thrown if the cells are not adjacent.</exception>
        public CellArc(Cell cell1, Cell cell2)
        {
            // Set the cells.
            this.cell1 = cell1;
            this.cell2 = cell2;

            // Ensure that the cells are adjacent.
            if (!((Math.Abs(cell1.x - cell2.x) == 1 && Math.Abs(cell1.y - cell2.y) == 0) || (Math.Abs(cell1.x - cell2.x) == 0 && Math.Abs(cell1.y - cell2.y) == 1)))
            {
                throw new ArgumentException("Cells in a cell arc must be adjacent! Cell1: " + cell1 + ", Cell2: " + cell2);
            }
        }

        /// <summary>
        /// Equals function to compare two arcs.
        /// </summary>
        /// <param name="other">The other cell to check equality with.</param>
        /// <returns>True if both arcs have the same pair of cells.</returns>
        public bool Equals(CellArc other)
        {
            return this.cell1 == other.cell1 && this.cell2 == other.cell2;
        }

        /// <summary>
        /// Function to print arc information.
        /// </summary>
        /// <returns>A string of arc information.</returns>
        public override string ToString()
        {
            return cell1 + " -> " + cell2;
        }
    }
}