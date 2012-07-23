﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace THOK.Wms.DbModel
{
    public class Cell
    {
        public Cell()
        {
            this.Storage = new List<Storage>();
            this.MoveBillDetailOutCells = new List<MoveBillDetail>();
            this.MoveBillDetailInCells = new List<MoveBillDetail>();
            this.CheckBillDetails = new List<CheckBillDetail>();
        }
        public string CellCode { get; set; }
        public string CellName { get; set; }
        public string ShortName { get; set; }
        public string CellType { get; set; }
        public int Layer { get; set; }
        public int Col { get; set; }
        public int ImgX { get; set; }
        public int ImgY { get; set; }
        public string Rfid { get; set; }
        public string WarehouseCode { get; set; }
        public string AreaCode { get; set; }
        public string ShelfCode { get; set; }
        public string DefaultProductCode { get; set; }
        public int MaxQuantity { get; set; }
        public string IsSingle { get; set; }
        public string Description { get; set; }
        public string LockTag { get; set; }
        public string IsActive { get; set; }
        public DateTime UpdateTime { get; set; }
        public byte[] RowVersion { get; set; }

        public virtual Warehouse Warehouse { get; set; }
        public virtual Area Area { get; set; }
        public virtual Shelf Shelf { get; set; }
        public virtual Product Product { get; set; }

        public virtual ICollection<Storage> Storage { get; set; }
        public virtual ICollection<MoveBillDetail> MoveBillDetailOutCells { get; set; }
        public virtual ICollection<MoveBillDetail> MoveBillDetailInCells { get; set; }
        public virtual ICollection<CheckBillDetail> CheckBillDetails { get; set; }
    }
}
