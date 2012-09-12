﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using THOK.Wms.Allot.Interfaces;
using Microsoft.Practices.Unity;
using THOK.WebUtil;
using THOK.Wms.Bll.Interfaces;

namespace Authority.Controllers.Wms.StockIn
{
    public class StockInBillAllotController : Controller
    {
        [Dependency]
        public IInBillAllotService InBillAllotService { get; set; }
        [Dependency]
        public IInBillDetailService InBillDetailService { get; set; }

        public ActionResult Search(string billNo, int page, int rows)
        {
            var result = InBillAllotService.Search(billNo, page, rows);
            return Json(result, "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotDelete(string billNo, long id)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotDelete(billNo, id, out strResult);
            string msg = bResult ? "删除分配明细成功" : "删除分配明细失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotEdit(string billNo, long id, string cellCode, int allotQuantity)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotEdit(billNo, id, cellCode, allotQuantity, out strResult);
            string msg = bResult ? "修改分配成功" : "修改分配失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotConfirm(string billNo)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotConfirm(billNo, out strResult);
            string msg = bResult ? "确认分配成功" : "确认分配失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotCancelConfirm(string billNo)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotCancelConfirm(billNo, out strResult);
            string msg = bResult ? "取消分配确认成功" : "取消分配确认失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotCancel(string billNo)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotCancel(billNo, out strResult);
            string msg = bResult ? "取消分配成功" : "取消分配失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllotAdd(string billNo, long id, string cellCode, int allotQuantity)
        {
            string strResult = string.Empty;
            bool bResult = InBillAllotService.AllotAdd(billNo, id, cellCode, allotQuantity, out strResult);
            string msg = bResult ? "添加分配成功" : "添加分配失败";
            return Json(JsonMessageHelper.getJsonMessage(bResult, msg, strResult), "text", JsonRequestBehavior.AllowGet);
        }

        #region /StockInBillAllot/CreateExcelToClient/
        public FileStreamResult CreateExcelToClient()
        {
            int page = 0, rows = 0;
            string billNo = Request.QueryString["billNo"];
            System.Data.DataTable dt = InBillDetailService.GetInBillDetail(page, rows, billNo);
            System.Data.DataTable dt2 = InBillAllotService.AllotSearch(page, rows, billNo);
            string strHeaderText = "入库单明细";
            string strHeaderText2 = "入库单分配明细";
            string exportDate = "导出时间：" + System.DateTime.Now.ToString("yyyy-MM-dd");
            string filename = strHeaderText + DateTime.Now.ToString("yyMMdd-HHmm-ss");
            Response.Clear();
            Response.BufferOutput = false;
            Response.ContentEncoding = System.Text.Encoding.GetEncoding("gb2312");
            Response.AddHeader("Content-Disposition", "attachment;filename=" + Uri.EscapeDataString(filename) + ".xls");
            Response.ContentType = "application/ms-excel";
            string[] str = {
                               "20",        //[0]大标题字体大小
                               "700",       //[1]大标题字体粗宽
                               "10",        //[2]列标题字体大小
                               "700",       //[3]列标题字体粗宽
                               "300",       //[4]excel中有数据表格的大小
                               "微软雅黑",  //[5]大标题字体
                               "Arial",     //[6]小标题字体
                           };
            System.IO.MemoryStream ms = THOK.Common.ExportExcel.ExportDT(dt, dt2, strHeaderText, strHeaderText2, str, exportDate);
            return new FileStreamResult(ms, "application/ms-excel");
        }
        #endregion
    }
}
