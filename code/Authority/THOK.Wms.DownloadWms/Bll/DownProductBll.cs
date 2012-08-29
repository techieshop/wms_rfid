using System;
using System.Collections.Generic;
using System.Text;
using THOK.Util;
using System.Data;
using THOK.WMS.DownloadWms.Dao;

namespace THOK.WMS.DownloadWms.Bll
{
    public class DownProductBll
    {
        #region ��Ӫϵͳ���²�Ʒ��Ϣ

        /// <summary>
        /// ���ز�Ʒ��Ϣ
        /// </summary>
        /// <returns></returns>
        public bool DownProductInfo()
        {
            bool tag = true;
            DataTable codedt = this.GetProductCode();
            string codeList = UtinString.StringMake(codedt, "custom_code");
            codeList = UtinString.StringMake(codeList);
            codeList = "BRAND_CODE NOT IN (" + codeList + ")";
            DataTable bradCodeTable = this.GetProductInfo(codeList);
            if (bradCodeTable.Rows.Count > 0)
            {
                DataSet brandCodeDs = this.Insert(bradCodeTable);
                this.Insert(brandCodeDs);
            }
            else
            {
                tag = false;
            }
            return tag;
        }

        /// <summary>
        /// ���ؾ��̲�Ʒ��Ϣ��
        /// </summary>
        /// <returns></returns>
        public DataTable GetProductInfo(string codeList)
        {
            using (PersistentManager dbPm = new PersistentManager("YXConnection"))
            {
                DownProductDao dao = new DownProductDao();
                dao.SetPersistentManager(dbPm);
                return dao.GetProductInfo(codeList);
            }
        }

        /// <summary>
        /// ��ѯ���̲�Ʒ���
        /// </summary>
        /// <returns></returns>
        public DataTable GetProductCode()
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                return dao.GetProductCode();
            }
        }

        /// <summary>
        /// ��ѯ������Ϣ
        /// </summary>
        /// <returns></returns>
        public DataTable ProductInfo()
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                return dao.ProductInfo();
            }
        }

        /// <summary>
        /// �����ݲ��뵽���ݿ�
        /// </summary>
        /// <param name="ds"></param>
        public void Insert(DataSet ds)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                dao.Insert(ds);
            }
        }

        /// <summary>
        /// ���ݼ�����λ��Ų�ѯ����
        /// </summary>
        /// <param name="ulistCode"></param>
        /// <returns></returns>
        public DataTable FindUnitCode(string ulistCode)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownUnitDao dao = new DownUnitDao();
                return dao.FindUnitCodeByUlistCode(ulistCode);
            }
        }

        public DataTable GetProductCode(string code)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                return dao.GetProductCode(code);
            }
        }


        public DataTable FindProductInfo(string productcode)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                return dao.FindProductInfo(productcode);
            }
        }

        public DataTable FindUnitListInfo(string unitListCode)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownProductDao dao = new DownProductDao();
                return dao.FindUnitListInfo(unitListCode);
            }
        }

        /// <summary>
        /// �������ݵ������
        /// </summary>
        /// <param name="brandTable"></param>
        /// <returns></returns>
        public DataSet Insert(DataTable brandTable)
        {
            DownUnitBll bll = new DownUnitBll();
            DataSet ds = this.GenerateEmptyTables();
            foreach (DataRow row in brandTable.Rows)
            {
                DataTable ulistCodeTable = this.FindUnitListCOde(row["BRAND_CODE"].ToString().Trim());               
                DataRow inbrddr = ds.Tables["WMS_PRODUCT"].NewRow();
                inbrddr["product_code"] = row["BRAND_N"];
                inbrddr["product_name"] = row["BRAND_NAME"];
                inbrddr["uniform_code"] = row["N_UNIFY_CODE"];
                inbrddr["custom_code"] = row["BRAND_CODE"];
                inbrddr["short_code"] = row["SHORT_CODE"];
                inbrddr["unit_list_code"] = ulistCodeTable.Rows[0]["unit_list_code"].ToString();
                inbrddr["unit_code"] = ulistCodeTable.Rows[0]["unit_code01"].ToString();
                inbrddr["supplier_code"] = "10863500";
                inbrddr["brand_code"] = "PP001";
                inbrddr["abc_type_code"] = "";
                inbrddr["product_type_code"] = row["BRAND_TYPE"];
                inbrddr["pack_type_code"] = "";
                inbrddr["price_level_code"] = "";
                inbrddr["statistic_type"] = "";
                inbrddr["piece_barcode"] = row["BARCODE_PIECE"];
                inbrddr["bar_barcode"] = row["BARCODE_BAR"];
                inbrddr["package_barcode"] = row["BARCODE_PACKAGE"];
                inbrddr["one_project_barcode"] = row["BARCODE_ONE_PROJECT"];
                inbrddr["buy_price"] = row["BUY_PRICE"];
                inbrddr["trade_price"] = row["TRADE_PRICE"];
                inbrddr["retail_price"] = row["RETAIL_PRICE"];
                inbrddr["cost_price"] = row["COST_PRICE"];
                inbrddr["is_filter_tip"] = row["IS_FILTERTIP"];
                inbrddr["is_new"] = row["IS_NEW"];
                inbrddr["is_famous"] = row["IS_FAMOUS"];
                inbrddr["is_main_product"] = row["IS_MAINPRODUCT"];
                inbrddr["is_province_main_product"] = row["IS_MAINPROVINCE"];
                inbrddr["belong_region"] = row["BELONG_REGION"];
                inbrddr["is_confiscate"] = row["IS_CONFISCATE"];
                inbrddr["is_abnormity"] = row["IS_ABNORMITY_BRAND"];
                inbrddr["description"] = "";
                inbrddr["is_active"] = row["IsActive"];
                inbrddr["update_time"] = DateTime.Now;
                ds.Tables["WMS_PRODUCT"].Rows.Add(inbrddr);
            }
            return ds;
        }

        /// <summary>
        /// �����ĸ��������ݱ���2���ϴ������̵ģ�2��
        /// </summary>
        /// <returns></returns>
        private DataSet GenerateEmptyTables()
        {
            DataSet ds = new DataSet();
            DataTable inbrtable = ds.Tables.Add("WMS_PRODUCT");
            inbrtable.Columns.Add("product_code");
            inbrtable.Columns.Add("product_name");
            inbrtable.Columns.Add("uniform_code");
            inbrtable.Columns.Add("custom_code");           
            inbrtable.Columns.Add("short_code");
            inbrtable.Columns.Add("unit_list_code");
            inbrtable.Columns.Add("unit_code");
            inbrtable.Columns.Add("supplier_code");
            inbrtable.Columns.Add("brand_code");
            inbrtable.Columns.Add("abc_type_code");
            inbrtable.Columns.Add("product_type_code");
            inbrtable.Columns.Add("pack_type_code");
            inbrtable.Columns.Add("price_level_code");
            inbrtable.Columns.Add("statistic_type");
            inbrtable.Columns.Add("piece_barcode");
            inbrtable.Columns.Add("bar_barcode");
            inbrtable.Columns.Add("package_barcode");
            inbrtable.Columns.Add("one_project_barcode");
            inbrtable.Columns.Add("buy_price");
            inbrtable.Columns.Add("trade_price");
            inbrtable.Columns.Add("retail_price");
            inbrtable.Columns.Add("cost_price");
            inbrtable.Columns.Add("is_filter_tip");
            inbrtable.Columns.Add("is_new");
            inbrtable.Columns.Add("is_famous");//һ�Ź���������
            inbrtable.Columns.Add("is_main_product");
            inbrtable.Columns.Add("is_province_main_product");
            inbrtable.Columns.Add("belong_region");
            inbrtable.Columns.Add("is_confiscate");
            inbrtable.Columns.Add("is_abnormity");
            inbrtable.Columns.Add("description");
            inbrtable.Columns.Add("is_active");
            inbrtable.Columns.Add("update_time");

            DataTable inpr = ds.Tables.Add("DWV_IINF_BRAND");
            inpr.Columns.Add("PRODUCTCODE");
            inpr.Columns.Add("PRODUCTN");
            inpr.Columns.Add("PRODUCTCLASS");
            inpr.Columns.Add("PRODUCTNAME");
            inpr.Columns.Add("SHORTNAME");
            inpr.Columns.Add("SUPPLIERCODE");
            inpr.Columns.Add("BARCODE");
            inpr.Columns.Add("ABCODE");
            inpr.Columns.Add("UNITCODE");
            inpr.Columns.Add("JIANTIAORATE");
            inpr.Columns.Add("TIAOBAORATE");
            inpr.Columns.Add("BAOZHIRATE");
            inpr.Columns.Add("JIANCODE");
            inpr.Columns.Add("TIAOCODE");
            inpr.Columns.Add("ZHICODE");
            inpr.Columns.Add("IS_BARAND");
            inpr.Columns.Add("MEMO");
            return ds;
        }
        #endregion

        #region ��Ӫϵͳ���²�Ʒ��Ϣ - �����˳�

        /// <summary>
        /// ����ģ��������λ�;��̱���ȥ�м��ȡ�ö�Ӧ�ļ�����λ
        /// </summary>
        /// <param name="unitcode"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public DataTable FindUnitListCOde(string product)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                DownUnitDao dao = new DownUnitDao();
                return dao.FindUnitListCOde(product);
            }
        }

        #endregion
    }
}