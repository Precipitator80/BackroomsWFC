using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace PrecipitatorWFC
{
    public class CellArc : MonoBehaviour
    {
        public Cell cell1, cell2;
        public CellArc(Cell cell1, Cell cell2)
        {
            this.cell1 = cell1;
            this.cell2 = cell2;
            if (!((Math.Abs(cell1.x - cell2.x) == 1 && Math.Abs(cell1.y - cell2.y) == 0) || (Math.Abs(cell1.x - cell2.x) == 0 && Math.Abs(cell1.y - cell2.y) == 1)))
            {
                throw new ArgumentException("Cells in a cell arc must be adjacent! Cell1: (" + cell1.x + "," + cell1.y + "), Cell2: (" + cell2.x + ", " + cell2.y + ")");
            }
        }
    }
}