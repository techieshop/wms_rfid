using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using THOK.WES.Dal;
using THOK.WES;
using THOK.WES.Interface;
using THOK.WES.Interface.Model;
using System.Collections;
using SignalR.Client;

namespace THOK.WES.View
{
    public partial class BaseTaskForm : THOK.AF.View.ToolbarForm
    {
        public delegate string TimerStateInMainThread();
        private ConfigUtil configUtil = new ConfigUtil();
        private ReadRfid rRfid = new ReadRfid();
        private string operateStorageName = "";
        private string targetStorageName = "";
        private string operateName = "";
        private string operateProductName = "";
        private int operatePieceQuantity = 0;
        private int operateBarQuantity = 0;
        private string url = @"http://59.61.87.212:8090/Task";

        /// <summary>
        /// 1：入库单；2：出库单；3：移库单；4：盘点单
        /// </summary>
        protected string BillTypes = "";

        //选择的主单；
        BillMaster BillMaster = null;
        BillMaster[] BillMasters = null;

        private string RfidReadProductCode = "";

        /// <summary>
        /// Real: 实时出库；NoReal: 非实时出库；
        /// </summary>
        private string OperateType = "";

        /// <summary>
        /// 操作区域 = 0：条烟柜；1～N ：货架层号；
        /// </summary>
        private string OperateAreas = "";

        /// <summary>
        /// 使用电子标签 = 0：不使用；1：使用；
        /// </summary>
        private string UseTag = "";

        /// <summary>
        /// 使用Rfid  = 0：不使用；1：手动使用；2：自动使用；
        /// </summary>
        private string UseRfid = "";

        /// <summary>
        /// 读取的托盘RFID号；
        /// </summary>
        private string RfidCode = "";

        /// <summary>
        /// 错误消息；
        /// </summary>
        private string errInfo;

        /// <summary>
        /// 串口；
        /// </summary>
        private string port;

        private Connection connection = null;
        private GridUtil gridUtil = null;
        public BaseTaskForm()
        {
            InitializeComponent();
            gridUtil = new GridUtil(dgvMain);
            url = configUtil.GetConfig("URL")["URL"];
            OperateAreas = configUtil.GetConfig("Layers")["Number"];
            UseRfid = configUtil.GetConfig("RFID")["USEDRFID"];
            connection = new Connection(url + @"/automotiveSystems");
            connection.Received += new Action<string>(connection_Received);
            connection.Closed += new Action(connection_Closed);

            if (configUtil.GetConfig("DeviceType")["Device"] == "0")
            {
                this.dgvMain.ColumnHeadersHeight = 40;
                this.dgvMain.RowTemplate.Height = 40;
                this.dgvMain.DefaultCellStyle.Font = new Font("宋体", 16);
                this.dgvMain.ColumnHeadersDefaultCellStyle.Font = new Font("宋体", 13);
                this.btnBatConfirm.Visible = false;
                UseTag = "0";
            }
            else if (configUtil.GetConfig("DeviceType")["Device"] == "1")
            {
                this.dgvMain.ColumnHeadersHeight = 40;
                this.dgvMain.RowTemplate.Height = 40;
                this.dgvMain.DefaultCellStyle.Font = new Font("宋体", 16);
                this.dgvMain.ColumnHeadersDefaultCellStyle.Font = new Font("宋体", 13);
                //this.btnBatConfirm.Visible = false;
                UseTag = "1";
            }
            else
            {
                this.dgvMain.ColumnHeadersHeight = 22;
                this.dgvMain.RowTemplate.Height = 22;
                this.dgvMain.DefaultCellStyle.Font = new Font("宋体", 10);
                this.dgvMain.ColumnHeadersDefaultCellStyle.Font = new Font("宋体", 10);
                UseTag = "1";
            }
            port = configUtil.GetConfig("RFID")["PORT"];            
        }

        //查询
        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string billNo = string.Empty;
                Task task = new Task(url);
                task.GetBillMasterCompleted += new Task.GetBillMasterCompletedEventHandler(delegate(bool isSuccess, string msg, BillMaster[] billMasters)
                {
                    ClosePlWailt();
                    if (billMasters != null)
                    {
                        switch (billMasters.Length)
                        {
                            case 0:
                                billNo = "";
                                break;
                            case 1:
                                billNo = billMasters[0].BillNo;
                                break;
                            default:
                                SelectDialog selectDialog = new SelectDialog(billMasters);
                                if (selectDialog.ShowDialog() == DialogResult.OK)
                                {
                                    billNo = selectDialog.SelectedBillID;
                                }
                                break;
                        }

                        List<BillMaster> listBill = new List<Interface.Model.BillMaster>();
                        int f = 0;
                        for (int i = 0; i < billMasters.Length; i++)
                        {
                            if (billNo.Contains(billMasters[i].BillNo))
                            {
                                f++;
                                listBill.Add(billMasters[i]);
                                this.BillMaster = billMasters[i];
                            }
                        }
                        BillMasters = new BillMaster[f];
                        listBill.CopyTo(BillMasters, 0);
                    }
                    if (!isSuccess)
                        MessageBox.Show(msg);
                    RefreshData();
                });
                task.SearchBillMaster(BillTypes);
                DisplayPlWailt();
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取数据失败，原因：" + ex.Message, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //刷新数据
        private void RefreshData()
        {
            if (BillMasters == null)
            {
                dgvMain.DataSource = null;
                return;
            }
            //sslBillID.Text = "单据号：" + BillMaster.BillNo + "                              ";
            sslOperator.Text = "操作员：" + Environment.MachineName;

            Task task = new Task(url);
            task.GetBillDetailCompleted += new Task.GetBillDetailCompletedEventHandler(delegate(bool isSuccess, string msg, BillDetail[] billDetails)
            {
                InTask = false;
                if (billDetails != null && billDetails.Length != 0)
                {
                    dgvMain.AutoGenerateColumns = false;
                    dgvMain.DataSource = billDetails;
                    foreach (BillDetail billDetail in billDetails)
                    {
                        if (billDetail.Status == "1")
                        {
                            InTask = true;
                            btnCancel.Enabled = true;
                            btnConfirm.Enabled = true;
                            btnBatConfirm.Enabled = true;
                        }
                        else
                        {
                            btnApply.Enabled = true;
                        }
                    }
                }
                else
                {
                    dgvMain.DataSource = null;
                }
                ClosePlWailt();
                dgvMain.ClearSelection();
            });
            task.SearchBillDetail(BillMasters, RfidReadProductCode, OperateType, OperateAreas, Environment.MachineName);
            DisplayPlWailt();
        }

        //申请
        private void btnApply_Click(object sender, EventArgs e)
        {
            string errString = string.Empty;
            List<string> listRfid = new List<string>();
            string productRfid = "";
            decimal quantityRfid = 0;
            try
            {
                if (UseRfid == "0")
                {
                    ApplyPublicMethod();
                }
                else
                {
                    if (BillTypes == "1")
                    {
                        while (listRfid.Count == 0 || listRfid == null)
                        {
                            DisplayPlWailt();
                            listRfid = rRfid.ReadTrayRfid(port, 115200, out errString);
                            Application.DoEvents();
                        }
                        RfidCode = RfidCode = listRfid[0].ToString();
                        Task task = new Task(url);
                        task.SearchRfidInfo(RfidCode);
                        task.GetRfidInfoCompleted += new Task.GetRfidInfoCompletedEventHandler(delegate(bool isSuccess, string msg, BillDetail[] billDetails)
                        {
                            if (billDetails != null && billDetails.Length != 0)
                            {
                                productRfid = billDetails[0].ProductCode;
                                quantityRfid = billDetails[0].PieceQuantity;
                            }
                            ApplyPublicMethod(UseRfid, RfidCode, productRfid, quantityRfid);
                        });
                    }
                    else
                    {
                        ApplyPublicMethod();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("申请错误：" + ex.Message + " ,其它:" + errString);
                RefreshData();
            }
        }

        //取消申请
        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvMain.SelectedRows.Count != 0)
                {
                    DisplayPlWailt();
                    rRfid.CloseCom();
                    IList<BillDetail> billDetails = new List<BillDetail>();
                    foreach (DataGridViewRow row in dgvMain.SelectedRows)
                    {
                        BillDetail billDetail = new BillDetail();
                        billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                        billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                        billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                        billDetail.Operator = Environment.MachineName;
                        billDetails.Add(billDetail);
                    }
                    BillDetail[] tmp = new BillDetail[billDetails.Count];
                    billDetails.CopyTo(tmp, 0);

                    Task task = new Task(url);
                    task.CancelCompleted += new Task.CancelCompletedEventHandler(delegate(bool isSuccess, string msg)
                    {
                        if (!isSuccess)
                            MessageBox.Show(msg);
                        RefreshData();
                    });
                    task.Cancel(tmp, UseTag);
                    RfidCode = "";
                }
                else
                    MessageBox.Show("请选择要取消的仓库作业。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("取消失败，原因：" + ex.Message, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //确认
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            ConfirmPubliceMethod();
            RefreshData();
        }

        //批量确认
        private void btnBatConfirm_Click(object sender, EventArgs e)
        {
            if (!UseRfid.Equals("0"))
            {
                MessageBox.Show("使用RFID无法批量完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("当前操作将批量确认选择的已申请的所有任务！", "提示",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                if (dgvMain.SelectedRows.Count != 0)
                {
                    DisplayPlWailt();
                    IList<BillDetail> billDetails = new List<BillDetail>();
                    foreach (DataGridViewRow row in dgvMain.SelectedRows)
                    {
                        BillDetail billDetail = new BillDetail();
                        billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                        billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                        billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                        billDetail.Operator = Environment.MachineName;

                        billDetail.OperatePieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                        billDetail.OperateBarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);

                        billDetails.Add(billDetail);
                    }
                    BillDetail[] tmp = new BillDetail[billDetails.Count];
                    billDetails.CopyTo(tmp, 0);

                    Task task = new Task(url);
                    task.ExecuteCompleted += new Task.ExecuteCompletedEventHandler(delegate(bool isSuccess, string msg)
                    {
                        if (!isSuccess)
                            MessageBox.Show(msg);
                        RefreshData();
                    });
                    task.Execute(tmp, UseTag);
                }
                else
                    MessageBox.Show("请选择要执行的仓库作业。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行失败，原因：" + ex.Message, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void DisplayPlWailt()
        {
            this.plWailt.Visible = true;
            this.plWailt.Left = (this.dgvMain.Width - this.plWailt.Width) / 2;
            this.plWailt.Top = (this.dgvMain.Height - this.plWailt.Height) / 2;
            this.btnSearch.Enabled = false;
            this.btnExit.Enabled = false;
            this.btnApply.Enabled = false;
            this.btnCancel.Enabled = false;
            this.btnConfirm.Enabled = false;
            this.btnBatConfirm.Enabled = false;
        }

        public void ClosePlWailt()
        {
            this.plWailt.Visible = false;
            this.btnSearch.Enabled = true;
            this.btnExit.Enabled = true;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void btnOpType_Click(object sender, EventArgs e)
        {
            if (btnOpType.Text != "正常")
            {
                btnOpType.Text = "正常";
                OperateType = "NoReal";
                connection.Stop();
            }
            else
            {
                btnOpType.Text = "实时";
                OperateType = "Real";

                //todo 连接实时作业信息服务器；
                connection.Start().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("Failed to start: {0}", task.Exception.GetBaseException());
                    }
                });
            }
            RefreshData();
        }

        delegate void RefreshTask();

        void connection_Received(string data)
        {
            if (data == "TaskStart")
            {
                Application.OpenForms[0].Invoke(new RefreshTask(RefreshData));
            }
        }

        void connection_Closed()
        {
            if (OperateType == "Real")
            {
                connection.Start();
            }
        }

        private bool isBcCompose = false;
        private void btnBcCompose_Click(object sender, EventArgs e)
        {
            btnBcCompose.Enabled = false;
            if (!isBcCompose && BillTypes == "3" && BillMasters != null)
            {
                Task task = new Task(url.Replace("Task", "StockMoveBill/GeneratePalletTag"));
                task.BcComposeCompleted += new Task.BcComposeEventHandler(delegate(bool isSuccess, string msg)
                {
                    dgvMain.Columns["PalletTag"].Visible = true;
                    if (!isSuccess)
                        MessageBox.Show(msg);
                    RefreshData();
                    btnBcCompose.Enabled = true;
                    isBcCompose = true;
                });
                task.BcCompose(BillMaster.BillNo);
            }
            else
            {
                dgvMain.Columns["PalletTag"].Visible = false;
                RefreshData();
                btnBcCompose.Enabled = true;
                isBcCompose = false;
            }
        }

        public void ApplyPublicMethod(string uRfid, string rfidId, string rfidProductCode, decimal rfidQuantity)
        {
            try
            {
                errInfo = "";
                bool isRfid = true;
                RfidCode = rfidId;
                decimal rfidQty = Convert.ToInt32(rfidQuantity);
                if (dgvMain.SelectedRows.Count != 0)
                {
                    foreach (DataGridViewRow row in dgvMain.Rows)
                    {
                        if (row.Cells["Status"].Value.ToString().Equals("1") && !uRfid.Equals("0"))
                        {
                            MessageBox.Show("使用RFID,只能申请一条数据。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    DisplayPlWailt();
                    IList<BillDetail> billDetails = new List<BillDetail>();

                    switch (uRfid)
                    {
                        case "0":
                            foreach (DataGridViewRow row in dgvMain.SelectedRows)
                            {
                                BillDetail billDetail = new BillDetail();
                                billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                                billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                                billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                                billDetail.PieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                                billDetail.BarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);
                                billDetail.Operator = Environment.MachineName;
                                billDetails.Add(billDetail);
                            }
                            isRfid = false;
                            break;
                        case "1":
                            foreach (DataGridViewRow row in dgvMain.SelectedRows)
                            {
                                if (rfidProductCode.Equals(row.Cells["ProductCode"].Value.ToString())
                                && rfidQty == Convert.ToInt32(row.Cells["PieceQuantity"].Value)
                                && row.Cells["Status"].Value.ToString().Equals("0"))
                                {
                                    BillDetail billDetail = new BillDetail();
                                    billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                                    billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                                    billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                                    billDetail.PieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                                    billDetail.BarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);
                                    billDetail.Operator = Environment.MachineName;
                                    billDetails.Add(billDetail);
                                    isRfid = false;
                                    break;
                                }
                            }
                            break;
                        case "2":
                            foreach (DataGridViewRow row in dgvMain.Rows)
                            {
                                //判断卷烟和数量与读取的是否一样。根据状态排除已经申请的货位。
                                if (rfidProductCode.Equals(row.Cells["ProductCode"].Value.ToString())
                                    && rfidQty == Convert.ToInt32(row.Cells["PieceQuantity"].Value)
                                    && row.Cells["Status"].Value.ToString().Equals("0"))
                                {
                                    BillDetail billDetail = new BillDetail();
                                    billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                                    billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                                    billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                                    billDetail.PieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                                    billDetail.BarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);
                                    billDetail.Operator = Environment.MachineName;
                                    billDetails.Add(billDetail);
                                    isRfid = false;
                                    break;
                                }
                            }
                            break;
                        default:
                            errInfo = "请查看使用RFID配置参数是否正确！";
                            break;
                    }

                    BillDetail[] tmp = new BillDetail[billDetails.Count];
                    billDetails.CopyTo(tmp, 0);

                    Task task = new Task(url);
                    task.ApplyCompleted += new Task.ApplyCompletedEventHandler(delegate(bool isSuccess, string msg)
                    {
                        if (!isSuccess)
                            errInfo += "  " + msg;
                        RefreshData();
                    });
                    task.Apply(tmp, UseTag);

                    if (isRfid)
                        MessageBox.Show("申请失败，原因：当前托盘卷烟和数量与作业数据不匹配或者 其他错误:  " + errInfo, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("请选择要执行的仓库作业。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("申请失败，原因：" + ex.Message, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyPublicMethod()
        {
            try
            {
                string storageRfide = "";
                if (dgvMain.SelectedRows.Count > 1 && !UseRfid.Equals("0"))
                {
                    MessageBox.Show("当前操作只允许操作一个任务！", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (DataGridViewRow row in dgvMain.Rows)
                {
                    if (row.Cells["Status"].Value.ToString().Equals("1") && !UseRfid.Equals("0"))
                    {
                        MessageBox.Show("使用RFID,只能申请一条数据。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                if (dgvMain.SelectedRows.Count != 0)
                {
                    DisplayPlWailt();
                    IList<BillDetail> billDetails = new List<BillDetail>();

                    foreach (DataGridViewRow row in dgvMain.SelectedRows)
                    {
                        BillDetail billDetail = new BillDetail();
                        billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                        billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                        billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                        billDetail.PieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                        billDetail.BarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);
                        billDetail.Operator = Environment.MachineName;
                        billDetails.Add(billDetail);
                        if (!UseRfid.Equals("0"))
                        {
                            storageRfide = row.Cells["StorageRfid"].Value.ToString();
                        }
                    }

                    BillDetail[] tmp = new BillDetail[billDetails.Count];
                    billDetails.CopyTo(tmp, 0);

                    Task task = new Task(url);
                    task.ApplyCompleted += new Task.ApplyCompletedEventHandler(delegate(bool isSuccess, string msg)
                    {
                        if (!isSuccess)
                            MessageBox.Show(msg);
                        RefreshData();
                    });
                    task.Apply(tmp, UseTag);
                }
                else
                    MessageBox.Show("请选择要执行的仓库作业。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //if (!RfidCode.Equals(storageRfide))
                //    MessageBox.Show("读取的rfid信息与申请的数据信息不一致，请重新申请", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("申请失败，原因：" + ex.Message, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshData();
            }
        }

        private void ConfirmPubliceMethod()
        {
            string errString = string.Empty;
            try
            {
                bool isRfid = true;
                decimal quantity = 0;
                List<string> listRfid = new List<string>();
                if (UseRfid != "0")
                {
                    foreach (DataGridViewRow row in dgvMain.SelectedRows)
                    {
                        quantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                    }
                    if (quantity == 30)
                    {
                        while (listRfid.Count == 0 || listRfid == null)
                        {
                            DisplayPlWailt();
                            listRfid = rRfid.ReadTrayRfid(port, 115200, out errString);
                            Application.DoEvents();
                        }
                    }
                }
                if (dgvMain.SelectedRows.Count > 1)
                {
                    MessageBox.Show("当前操作只允许操作一个任务！", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (listRfid.Count == 0 && !UseRfid.Equals("0") && quantity==30)
                {
                    MessageBox.Show("读取RFID信息失败！请取消任务重新申请！", "提示",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;

                }
                IList<BillDetail> billDetails = new List<BillDetail>();
                BillDetail billDetail = new BillDetail();
                switch (UseRfid)
                {
                    case "0":
                        if (dgvMain.SelectedRows.Count == 1)
                        {
                            foreach (DataGridViewRow row in dgvMain.SelectedRows)
                            {
                                ConfirmMethod(row, billDetail, billDetails, RfidCode);
                                isRfid = false;
                            }
                        }
                        else
                            errInfo = "请选择一条数据确认！";
                        break;
                    case "1":
                        if (dgvMain.SelectedRows.Count == 1)
                        {
                            foreach (DataGridViewRow row in dgvMain.SelectedRows)
                            {
                                string cellRfid = row.Cells["CellRfid"].Value.ToString();
                                if (BillTypes == "3")
                                {
                                    if (!listRfid.Contains(row.Cells["StorageRfid"].Value.ToString())&& quantity==30)//移出的库存(托盘)的rfid
                                    {
                                        MessageBox.Show("读取RFID信息与数据不一致！请检查托盘卷烟与数据是否符合！", "提示",
                                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        return;
                                    }
                                    cellRfid = row.Cells["TargetStorageRfid"].Value.ToString();//移入的货位rfid
                                }
                                if (listRfid.Contains(cellRfid) || listRfid.Count==0)
                                {
                                    ConfirmMethod(row, billDetail, billDetails, RfidCode);
                                    isRfid = false;
                                }
                            }
                        }
                        else
                            errInfo = "请选择一条数据确认！";
                        break;
                    case "2":
                        foreach (DataGridViewRow row in dgvMain.Rows)
                        {
                            string cellRfid = row.Cells["CellRfid"].Value.ToString();
                            if (BillTypes == "3")
                            {
                                if (!listRfid.Contains(row.Cells["StorageRfid"].Value.ToString()) && quantity == 30)
                                {
                                    MessageBox.Show("读取RFID信息失败！请取消任务重新申请！", "提示",
                                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                                cellRfid = row.Cells["TargetStorageRfid"].Value.ToString();
                            }
                            if (listRfid.Contains(cellRfid))
                            {
                                ConfirmMethod(row, billDetail, billDetails, RfidCode);
                                isRfid = false;
                                break;
                            }
                        }
                        break;
                    default:
                        errInfo = "请查看使用RFID配置参数是否正确！";
                        break;
                }
                if (isRfid)
                    MessageBox.Show("完成确认失败，原因：找不到与货位RFID相等的数据！其他错误：" + errInfo + " ," + errString);
                else
                    RfidCode = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行失败，原因：" + ex.Message + "," + errString, "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ConfirmMethod(DataGridViewRow row, BillDetail billDetail, IList<BillDetail> billDetails, string rfidID)
        {
            if (row.Cells["StatusName"].Value.ToString() == "已申请")
            {
                billDetail.BillNo = row.Cells["BillNo"].Value.ToString();
                billDetail.BillType = row.Cells["@BillType"].Value.ToString();
                billDetail.DetailID = Convert.ToInt32(row.Cells["DetailID"].Value);
                billDetail.Operator = Environment.MachineName;
                billDetail.StorageRfid = rfidID;//托盘rfid
                billDetail.OperatePieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                billDetail.OperateBarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);

                operateStorageName = row.Cells["Storage"].Value.ToString();
                targetStorageName = row.Cells["TargetStorage"].Value.ToString();
                operateName = row.Cells["BillTypeName"].Value.ToString();
                operateProductName = row.Cells["ProductName"].Value.ToString();
                operatePieceQuantity = Convert.ToInt32(row.Cells["PieceQuantity"].Value);
                operateBarQuantity = Convert.ToInt32(row.Cells["BarQuantity"].Value);
            }

            ConfirmDialog confirmForm = new ConfirmDialog(BillTypes, operateStorageName, targetStorageName, operateName, operateProductName);
            confirmForm.Piece = operatePieceQuantity;
            confirmForm.Item = operateBarQuantity;
            if (confirmForm.ShowDialog() == DialogResult.OK)
            {
                DisplayPlWailt();

                if (BillTypes == "4")
                {
                    billDetail.OperatePieceQuantity = confirmForm.Piece;
                    billDetail.OperateBarQuantity = confirmForm.Item;
                }

                //todo RFID 确认，及RFID 记录；

                billDetails.Add(billDetail);
                BillDetail[] tmp = new BillDetail[billDetails.Count];
                billDetails.CopyTo(tmp, 0);

                Task task = new Task(url);
                task.ExecuteCompleted += new Task.ExecuteCompletedEventHandler(delegate(bool isSuccess, string msg)
                {
                    if (!isSuccess)
                        MessageBox.Show(msg);
                    RefreshData();
                });
                task.Execute(tmp, UseTag);
            }
        }

        public void ReadRfidCycle()
        {
            try
            {
                if (UseRfid.Equals("2"))
                {
                    if (RfidCode.Equals(""))
                        btnApply_Click(null, null);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void CyleTimer_Tick(object sender, EventArgs e)
        {
            if (BillMasters != null)
            {
                this.ReadRfidCycle();
            }
        }

        private decimal sumQuantity = 0;
        int f = 0;
        private int y = 0;
        private void dgvMain_CellPaintinga(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if ((e.ColumnIndex == this.dgvMain.Columns["Storage"].Index && e.RowIndex >= 0))
            {
                Brush datagridBrush = new SolidBrush(dgvMain.GridColor);
                SolidBrush groupLineBrush = new SolidBrush(e.CellStyle.BackColor);

                using (Pen datagridLinePen = new Pen(datagridBrush))
                {
                    // 清除单元格
                    e.Graphics.FillRectangle(groupLineBrush, e.CellBounds);
                    if (e.RowIndex < dgvMain.Rows.Count - 1 && dgvMain.Rows[e.RowIndex + 1].Cells[e.ColumnIndex].Value != null && dgvMain.Rows[e.RowIndex + 1].Cells[e.ColumnIndex].Value.ToString() != e.Value.ToString())
                    {
                        //绘制底边线
                        e.Graphics.DrawLine(datagridLinePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                        // 画右边线
                        e.Graphics.DrawLine(datagridLinePen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);

                        if (e.Value != null)
                        {
                            var cell = this.dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["PieceQuantity"].Index];
                            sumQuantity = sumQuantity + Convert.ToDecimal(this.dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["PieceQuantity"].Index].Value.ToString());
                            if (y != 0)
                            {
                                y = (y + e.CellBounds.Y + 10) / 2;
                            }
                            else
                            {
                                y = e.CellBounds.Y + 2;
                            }
                            if (cell.Tag == null)
                            {
                                cell.Tag = new object[] { sumQuantity, y };
                            }
                            object[] tag = (object[])cell.Tag;
                            dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["总数"].Index].Value = tag[0].ToString();
                            sumQuantity = 0;
                            y = 0;
                        }
                    }
                    else
                    {
                        sumQuantity = sumQuantity + Convert.ToDecimal(this.dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["PieceQuantity"].Index].Value.ToString());
                        if (y == 0)
                        {
                            y = e.CellBounds.Y;
                        }
                        // 画右边线
                        e.Graphics.DrawLine(datagridLinePen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
                    }

                    //对最后一条记录只画底边线
                    if (e.RowIndex == dgvMain.Rows.Count - 1)
                    {
                        if (e.Value != null)
                        {
                            if (dgvMain.Rows[e.RowIndex - 1].Cells[e.ColumnIndex].Value.ToString() != e.Value.ToString())
                            {
                                sumQuantity = 0;
                            }
                            var cell = this.dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["PieceQuantity"].Index];
                            sumQuantity = sumQuantity + Convert.ToDecimal(this.dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["PieceQuantity"].Index].Value.ToString());
                            if (y != 0)
                            {
                                y = (y + e.CellBounds.Y + 10) / 2;
                            }
                            else
                            {
                                y = e.CellBounds.Y + 2;
                            }
                            if (cell.Tag == null)
                            {
                                cell.Tag = new object[] { sumQuantity, y };
                            }
                            object[] tag = (object[])cell.Tag;
                            dgvMain.Rows[e.RowIndex].Cells[this.dgvMain.Columns["总数"].Index].Value = sumQuantity;//tag[0].ToString();
                            sumQuantity = 0;
                            y = 0;
                        }
                        //绘制底边线
                        e.Graphics.DrawLine(datagridLinePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    // 填写单元格内容，相同的内容的单元格只填写第一个                        
                    if (e.Value != null)
                    {
                        if (e.RowIndex > 0 && dgvMain.Rows[e.RowIndex - 1].Cells[e.ColumnIndex].Value.ToString() == e.Value.ToString())
                        {
                        }
                        else
                        {
                            e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, Brushes.Black, e.CellBounds.X + 2, e.CellBounds.Y + 5, StringFormat.GenericDefault);
                        }
                    }
                    e.Handled = true;
                }
            }
        }
    }
}

