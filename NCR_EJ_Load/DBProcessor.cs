using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Collections;


namespace NCR_EJ_Load
{
    class DBProcessor
    {
                /// <summary>
        /// Define  Global Variables , Oracle Connectio, Command and Adapter in addition to Object from Debug Class
        /// </summary>
      
        OracleConnection oraCon = new OracleConnection();
        /// <summary>
        /// 
        /// </summary>
        OracleCommand oraCom = new OracleCommand();
        /// <summary>
        /// 
        /// </summary>
        OracleDataAdapter oraDa = new OracleDataAdapter();
        /// <summary>
        /// 
        /// </summary>
        Logger objLogger = new Logger();
        public static string bank_bin;
        //MOH - 16-02-2015 - Starts - Deposit Currency
        public string BNA_DEP_CCY = "682";
        //MOH - 16-02-2015 - Ends - Deposit Currency
        public DBProcessor(string Ora_Con_String, string global_bank_bin)
        {
            oraCon.ConnectionString = Ora_Con_String;
            bank_bin = global_bank_bin;
        } 

        
        public void OpenConnection()
        {
            try
            {
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();
                Console.WriteLine("Successfully Connected to DB");
                objLogger.LogMsg("Successfully Connected to DB");

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 008: Funciton OpenConnection: , ex Source: " + ex.Source + ", Message: " + ex.Message + " " + oraCon.ConnectionString);
            }
        }

        public void CloseConnection()
        {
            if (oraCon.State != ConnectionState.Closed)
                oraCon.Close();
        }

        public bool Insert_EJ_Process_Log(string RECORD_DATE, string sFileNumber, string  FILE_DATE, 
            string  PROC_START_TIME, string  PROC_END_TIME, string  PROCESSING_STATUS,
            int RECORDS_LOADED, int RECORDS_REJECTED, int TOTAL_RECORDS, out int iEPD_CODE)
        {
            try
            {
                //int iEPD_CODE = 0;
                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT EPD_CODE FROM EJ_PROCESS_LOG WHERE record_Date= '" +
                    (DateTime.Now.Year.ToString().Substring(2, 2) + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0')) + "'";
                    //+ "' AND ATM_TYPE = '" + ATM_TYPE + "'";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();

                if (dr.HasRows)
                {
                    objLogger.LogMsg("WARNING: EJ_PROCESS_LOG is already having a record with record date:  " + RECORD_DATE + ",But a new record will be added");
                }
                  oraCom.CommandText = "INSERT INTO EJ_PROCESS_LOG (RECORD_DATE,  FILE_DATE, PROC_START_TIME, PROC_END_TIME,"+ 
                        "PROCESSING_STATUS, RECORDS_LOADED, RECORDS_REJECTED, TOTAL_RECORDS, FILE_NUMBER) VALUES('" +
                        RECORD_DATE +"', '" + FILE_DATE + "', '" + PROC_START_TIME + "', '"+ PROC_END_TIME + "', '"+
                        PROCESSING_STATUS + "', "+ RECORDS_LOADED + ", "+ RECORDS_REJECTED +", "+ TOTAL_RECORDS + ",'" + sFileNumber +"') returning EPD_CODE into :EPD_CODE";
                    

                    oraCom.Parameters.Add(new OracleParameter
                    {
                        ParameterName = ":EPD_CODE",
                        OracleType = OracleType.Number,
                        Direction = ParameterDirection.Output
                    });

                    oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                        oraCon.Open();

                    if (oraCom.ExecuteNonQuery() > 0)
                    {
                        iEPD_CODE = Convert.ToInt32(oraCom.Parameters[":EPD_CODE"].Value);
                        objLogger.LogMsg("EJ_PROCESS_LOG updated  successfully with record date as " + RECORD_DATE + " and EPD_CODE as " + iEPD_CODE.ToString() );
                        return true;
                    }
                    else
                    {
                        objLogger.LogMsg("ERROR: EJ_PROCESS_LOG could not be updated  for record date:  " + RECORD_DATE);
                        iEPD_CODE = 0;
                        return false;
                    }

                
                
            }//try
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  Insert_EJ_Process_Log Code 008, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                iEPD_CODE = 0;
                return false;
            }
            finally
            {
                oraCon.Close();
            }


        }


        public int TotalRcds_ATM_DATA_TABLE()
        {
            try
            {
                //int iEPD_CODE = 0;
                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT count(*) FROM ATM_DATA_TABLE ";
                ;
                //+ "' AND ATM_TYPE = '" + ATM_TYPE + "'";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();

                dr.Read();


                if (dr.HasRows)
                {
                    int iCount =  int.Parse( dr.GetValue(0).ToString());
                    objLogger.LogMsg("TotalRcds_ATM_DATA_TABLE: EJ Process has total atm records:  " + iCount);
                    return iCount;
                }

                else
                {

                    objLogger.LogMsg("ERROR : TotalRcds_ATM_DATA_TABLE: EJ Process has total found no atms  " );

                    return 0;
                }



            }//try
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  TotalRcds_ATM_DATA_TABLE Code 001, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return 0;
            }
            finally
            {
                oraCon.Close();
            }


        }



        public bool Chk_EJ_Process_Log_for_PrevDay(string RECORD_DATE, ref int iRemainingRcds)
        {
            try
            {
                //int iEPD_CODE = 0;
                int iTotalAtmRcds = TotalRcds_ATM_DATA_TABLE();
                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT EPD_CODE, RECORDS_LOADED FROM EJ_PROCESS_LOG WHERE PROCESSING_STATUS in ('002','001') and file_date= (select to_char(to_date('" + RECORD_DATE + "','yymmdd')-1,'yymmdd') from dual)";
                ;
                //+ "' AND ATM_TYPE = '" + ATM_TYPE + "'";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                
                dr.Read();

               
                if (dr.HasRows)
                {
                    int iRcdsLoaded = int.Parse(dr.GetValue(1).ToString());
                    
                    
                    iRemainingRcds = iTotalAtmRcds - iRcdsLoaded;

                    objLogger.LogMsg("Chk_EJ_Process_Log_for_Prev: EJ Process has previous day exection, so continuing for date:  " + RECORD_DATE + ",But a new record will be added");
                    return true;
                }

                else
                {
                    iRemainingRcds = iTotalAtmRcds;
                    objLogger.LogMsg("ERROR :Chk_EJ_Process_Log_for_Prev: EJ_PROCESS_LOG does not have record for previous day while running for date:  " + RECORD_DATE);

                    return false;
                }



            }//try
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  Chk_EJ_Process_Log_for_Prev Code 001, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }


        }

        public bool Insert_ATM_If_Missing(string teriminal_id_16)
        {
            try
            {

                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT term_id FROM ATM_EDC_ATM WHERE term_id= '" + teriminal_id_16.Trim() + "'";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();

                if (!dr.HasRows)
                {
                    oraCom.CommandText = "INSERT INTO ATM_EDC_ATM (term_id) VALUES('" + teriminal_id_16.Trim() + "')";
                    oraCom.Connection = oraCon;
                    if (oraCon.State != ConnectionState.Open)
                        oraCon.Open();

                    if (oraCom.ExecuteNonQuery() > 0)
                    {
                        objLogger.LogMsg("Terminal " + teriminal_id_16 + " was added successfully");
                        return true;
                    }
                    else return false;

                }
                else
                {
                    //objLogger.LogMsg("Terminal " + teriminal_id_16 + " is already added");
                    return true;
                }
             }//try
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  Insert_ATM_If_Missing Code 021, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();

                return false;
            }
            finally
            {
                oraCon.Close();
            }


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <param name="date3"></param>
        /// <param name="time1"></param>
        /// <param name="time2"></param>
        /// <param name="time3"></param>
        /// <param name="pcode"></param>
        /// <param name="amt"></param>
        /// <param name="luno"></param>
        /// <param name="txntype"></param>
        /// <param name="trace"></param>
        /// <param name="termid"></param>
        /// <param name="disp_denom"></param> /**
        /// <param name="fromacctno"></param>
        /// <param name="toacctno"></param>
        /// <param name="pan"></param>
        /// <param name="txn_cur"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool InsertEJ_Transaction( string date1, string date2, string  date3,
        string time1, string  time2, string time3,
        string pcode,  string amt, string  luno,  string txntype, string  trace, string  termid,
         string disp_denom, string fromacctno, string toacctno, string pan, string txn_cur, string host_resp, string filename, int iFile_Number, string global_term_id, string global_date, string ETT_Code)
       // Coin Changes adding ETT_Code
        { 
            try
            {
                //string bin = pan.Substring(0, 6);
                //string respcode = "000";
                string fdate = global_date; ;
                string f_termid = global_term_id.Substring(3, 4);
                string filename2 = "";
                
                if (filename.Length > 8)
                {
                 //   fdate = filename.Substring(17, 8);
                   // f_termid = filename.Substring(12, 4);
                     filename2 = filename;
                }
               
                oraCom = new OracleCommand();
               
                // Coin deposit EXTD_TXN_TYPE added

                oraCom.CommandText = "INSERT INTO ATM_EDC_TXN(bin,pan,trace,amount, txn_cur, txn_type,EXTD_TXN_TYPE, resp_code, dispenser, tran_date, tran_time, rec_date, term_id, termid_edc, account, to_account, host_resp, file_name,file_number)" +
                //" VALUES ( '"     + bin + "','" +  pan + "', '" + trace + "', '" + amt + "', '" + txn_cur + "', '" + txntype + "', '" +
                //" VALUES ( '" + bank_bin + "','" + pan + "', '" + trace + "', '" + amt + "', '" + txn_cur + "', '" + pcode + "', '" +
                " VALUES ( '" + bank_bin + "','" + pan.Trim() + "', '" + trace.Trim() + "', '" + amt + "', '" + txn_cur + "', '" + pcode + "', '" 
                + ETT_Code + "', '" + // Coin Changes 180325  ETT_Code only for Cash & Coin Deposit
                 host_resp + "', '" + disp_denom + "', " + "to_date('" + date2 + "','MMDDYY'),'" + time2 + "', " + "to_date('" + fdate + "','yyyymmdd'), '" +
                //  termid + "', '" + f_termid +"','" + fromacctno + "', '"  + toacctno  + "', to_number('" + host_resp +"'),'" + filename2 + "',"+iFile_Number+")";
                  termid.Trim() + "', '" + f_termid + "','" + fromacctno + "', '" + toacctno + "', to_number('" + host_resp + "'),'" + filename2 + "'," + iFile_Number + ")";
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();
                
                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Transaction Code 001, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
            
        }
        public bool UpdateFileProcessStatus(string term_id, string txn_count, string stat_count,
                 string rec_date, string ej_load_Status, string filename, string sFileNumber)
        {
            
            try
            {
               

                oraCom = new OracleCommand();
                
                //DateTime.Now.Date.ToShortDateString() + " " + DateTime.Now.ToLongTimeString()

                oraCom.CommandText = "INSERT INTO ATM_EDC_HISTORY(term_id,txn_count,stat_count,file_name,proc_date,rec_date,EJ_LOAD_STATUS, FILENUMBER )" +
              //  " VALUES ( '" + term_id + "','" + txn_count + "', '" + stat_count + "', '" + filename.Trim() + "', sysdate" +  /*DateTime.Now +*/  ", " +
                " VALUES ( '" + term_id.Trim() + "','" + txn_count + "', '" + stat_count + "', '" + filename.Trim() + "', sysdate" +  /*DateTime.Now +*/  ", " +
                 "to_date('" + rec_date + "','YYYYMMDD'),'" + ej_load_Status +"','"+ sFileNumber + "')";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  UpdateFileProcessStatus Code 007, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
            //return true;
            // "Insert Into EJ_PROCESS_LOG Values(,1,,'" + + "','','','','')";
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="term_id"></param>
        /// <param name="term_trace"></param>
        /// <param name="atm_type"></param>
        /// <param name="status"></param>
        /// <param name="stat_desc"></param>
        /// <param name="stat_date"></param>
        /// <param name="stat_time"></param>
        /// <param name="severity"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        //UMER_MOD 140521 added txn_prev_trace in insert
        public bool InsertEJ_Status(string term_id, string term_trace, string atm_type,
     string status, string stat_desc, string stat_date,
     string stat_time, string severity,string filename,int iFileNumber,string global_term_id, string global_date, string prev_txn_trace)
        {
            //INSERT INTO ATM_EDC_STATUS(bin,term_id,terminal_id_4digit,term_trace,atm_type,status,stat_desc,stat_date,stat_time,rec_date,severity)
            try
            {
                //string bin = "589206";
                string fdate = global_date; ;
                //string f_termid = global_term_id.Substring(3,4);
                string f_termid = global_term_id.Trim().Substring(3, 4);
                //string filename2 = "atm0000_20010101";

                /*if (filename.Length > 8)
                {
                    fdate = filename.Substring(17, 8);
                    f_termid = filename.Substring(12, 4);
                   // filename2 = filename;
                }*/
                

                oraCom = new OracleCommand();
                if (stat_desc.Length > 500)
                {
                    stat_desc = stat_desc.Substring(0, 499);
                }
                oraCom.CommandText = "INSERT INTO ATM_EDC_STATUS(bin,term_id,terminal_id_4digit,term_trace,atm_type,status,stat_desc,stat_date,stat_time,rec_date,severity,file_number,txn_prev_trace)" +
                //" VALUES ( '" + bank_bin + "','" + term_id + "', '" + f_termid + "', '" + term_trace + "', '" + atm_type + "', '" + status + "', '" +
                " VALUES ( '" + bank_bin + "','" + term_id.Trim() + "', '" + f_termid + "', '" + term_trace.Trim() + "', '" + atm_type + "', '" + status + "', '" +
                 stat_desc +  "', " + "to_date('" + stat_date + "','MM/DD/YY'),'" + stat_time + "', " + "to_date('" + fdate + "','yyyymmdd'), " +
                   severity + "," +iFileNumber + ",'"+prev_txn_trace +"')";
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Status Code 002, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
            
        }

     /// <summary>
     /// 
     /// </summary>
     /// <param name="record_id"></param>
     /// <returns></returns>
        public bool UpdateEJ_Replinishment_BNA_Clear(string record_id, string date, string time)
        {
            try
            {

                oraCom = new OracleCommand();



                oraCom.CommandText = "UPDATE ATM_EDC_RECON SET counters_print_flag ='C' , msg_date =" +"to_date('" + date + "','MM/DD/YY'), msg_time='"+ time +"'"  +"where record_id = '" + record_id + "'";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  UpdateEJ_Replinishment_BNA_Clear Code 006, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
        }
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="teriminal_id_16"></param>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="last_cleared_date"></param>
        /// <param name="last_cleared_time"></param>
        /// <param name="clear_counter_flag"></param>
        /// <param name="cash_dep_cas1"></param>
        /// <param name="cash_dep_cas2"></param>
        /// <param name="cash_dep_cas3"></param>
        /// <param name="cash_dep_cas4"></param>
        /// <param name="cash_ret_cas1"></param>
        /// <param name="cash_ret_cas2"></param>
        /// <param name="cash_ret_cas3"></param>
        /// <param name="cash_ret_cas4"></param>
        /// <param name="record_type"></param>
        /// <param name="filename"></param>
        /// <param name="record_id_in"></param>
        /// <returns></returns>
        
        //MOH - 16-02-2015 - Starts - Modified the function definition to log the values for Currency, Reject values and Total_dep_amt

        /*public bool InsertEJ_Replinishment_BNA_Print(string teriminal_id_16, string date, string time,
            string last_cleared_date, string last_cleared_time, char clear_counter_flag,
            string cash_dep_cas1, string cash_dep_cas2, string cash_dep_cas3, string cash_dep_cas4,
            string cash_ret_cas1, string cash_ret_cas2, string cash_ret_cas3, string cash_ret_cas4,
            string record_type, string filename, int iFileNumber, out string record_id_in, string global_term_id, string global_date)*/

        public bool InsertEJ_Replinishment_BNA_Print(string teriminal_id_16, string date, string time,
            string last_cleared_date, string last_cleared_time, char clear_counter_flag,
            string cash_dep_cas1, string cash_dep_cas2, string cash_dep_cas3, string cash_dep_cas4,
            string cash_ret_cas1, string cash_ret_cas2, string cash_ret_cas3, string cash_ret_cas4,
            string cash_rej_cas1, string cash_rej_cas2, string cash_rej_cas3, string cash_rej_cas4,string tot_cash_dep_amt,
            string record_type, string filename, int iFileNumber, out string record_id_in, string global_term_id, string global_date)
        // MOH - 16-02-2015 - Ends
        {
            try
            {
                //string bin = "589206";

                string fdate = global_date; ;
                string f_termid = global_term_id.Substring(3, 4);
                /*
                if (filename.Length > 8)
                {
                    fdate = filename.Substring(17, 8);
                    f_termid = filename.Substring(12, 4);

                }*/
                string record_id = "";
                
                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT lpad(to_char(recnum_seq.nextval),7,'0' ) FROM DUAL";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();
                record_id = (string)dr.GetValue(0);

                record_id_in = "";
                oraCom.CommandText = "INSERT INTO ATM_EDC_RECON( bin,terminal_id,terminal_id_4digit,msg_date,msg_time,prev_cleared_date," +
                 "prev_cleared_time,counters_print_flag,record_date,record_id," +
                 "cash_dep_cas1,cash_dep_cas2,cash_dep_cas3,cash_dep_cas4,cash_dep_ret1,cash_dep_ret2,cash_dep_ret3,cash_dep_ret4,cash_rej_cas1," + 
                 "cash_rej_cas2,cash_rej_cas3,cash_rej_cas4,cash_cur_cas1,cash_cur_cas2,cash_cur_cas3,cash_cur_cas4,tot_cash_dep_amt,record_type,file_number)" +
                 "VALUES('" + bank_bin + "','" + teriminal_id_16 + "', '" + f_termid + "', " + "to_date('" + date + "','MM/DD/YY'),'" + time + "', " + "to_date('" + last_cleared_date + "','MM/DD/YY'),'" +
                 last_cleared_time + "', '" + clear_counter_flag + "', " + "to_date('" + fdate + "','yyyymmdd'),'" +
                 record_id + "', '" + cash_dep_cas1.Trim() + "', '" + cash_dep_cas2.Trim() + "', '" + cash_dep_cas3.Trim() + "', '" + cash_dep_cas4.Trim() + "', '"
                 + cash_ret_cas1.Trim() + "', '" + cash_ret_cas2.Trim() + "', '" + cash_ret_cas3.Trim() + "', '" + cash_ret_cas4.Trim() + "', '"
                 + cash_rej_cas1.Trim() + "', '" + cash_rej_cas2.Trim() + "', '" + cash_rej_cas3.Trim() + "', '" + cash_rej_cas4.Trim() + "', '"
                 + BNA_DEP_CCY.Trim() + "', '" + BNA_DEP_CCY.Trim() + "', '" + BNA_DEP_CCY.Trim() + "', '" + BNA_DEP_CCY.Trim() + "', '"
                 + tot_cash_dep_amt.Trim() + "', '"
                 + record_type + "',"+iFileNumber+ ")";
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    record_id_in = record_id;
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Replinishment_BNA_Print Code 005, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                record_id_in = "";
                return false;
            }
            finally
            {
                oraCon.Close();
            }

            //return true;
            // "Insert Into EJ_PROCESS_LOG Values(,1,,'" + + "','','','','')";
        }
        // Coin New Funciotn Starts 
        public bool InsertEJ_Replinishment_Coin(string teriminal_id_16, string date, string time,
      string last_cleared_date, string last_cleared_time, string Coin_clear_counter_flag, 
                           string[] COIN_DEP_BIN,
                           string COIN_DEP_RET,
                           string TOT_COIN_DEP_AMT,
                           string COIN_DEP_AMT ,
                           
    
      string record_type, string filename, int iFileNumber, out string record_id_in, string global_term_id, string global_date)
        // MOH - 16-02-2015 - Ends
        {
            try
            {
                //string bin = "589206";

                string fdate = global_date; ;
                string f_termid = global_term_id.Substring(3, 4);
                /*
                if (filename.Length > 8)
                {
                    fdate = filename.Substring(17, 8);
                    f_termid = filename.Substring(12, 4);

                }*/
                string record_id = "";

                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT lpad(to_char(recnum_seq.nextval),7,'0' ) FROM DUAL";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();
                record_id = (string)dr.GetValue(0);

                record_id_in = "";

                string insertSQL = "INSERT INTO ATM_EDC_RECON( bin,terminal_id,terminal_id_4digit,msg_date,msg_time,prev_cleared_date," +
                 "prev_cleared_time,counters_print_flag,record_date,record_id," +
                  "record_type,file_number,coin_dep_ret,tot_coin_dep_amt,coin_dep_amt";
                 for (int c = 1; c <= COIN_DEP_BIN.Length; c++)
                   insertSQL += ",coin_dep_bin"+c;


                 insertSQL += ") VALUES('" + bank_bin + "','" + teriminal_id_16 + "', '" + f_termid + "', " + "to_date('" + date + "','MM/DD/YY'),'" + time + "', " + "to_date('" + last_cleared_date + "','MM/DD/YY'),'" +
                last_cleared_time + "', '" + Coin_clear_counter_flag + "', " + "to_date('" + fdate + "','yyyymmdd'),'" +
                record_id + "', '" + record_type + "'," + iFileNumber +
                ", '" + COIN_DEP_RET + "', '" + TOT_COIN_DEP_AMT + "', '" + COIN_DEP_AMT;

                  for (int c = 1; c <= COIN_DEP_BIN.Length; c++)
                      insertSQL += "','" + COIN_DEP_BIN[c-1];

                insertSQL+="') ";


                oraCom.CommandText = insertSQL;
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    record_id_in = record_id;
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Replinishment_BNA_Print Code 005, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                record_id_in = "";
                return false;
            }
            finally
            {
                oraCon.Close();
            }

            //return true;
            // "Insert Into EJ_PROCESS_LOG Values(,1,,'" + + "','','','','')";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="record_id"></param>
        /// <returns></returns>
        public bool UpdateEJ_Replinishment_DISP_Clear(string record_id,string date, string time)
        {
            try
            {
                
                oraCom = new OracleCommand();



                oraCom.CommandText = "UPDATE ATM_EDC_RECON SET counters_print_flag ='C' , msg_date =" + "to_date('" + date + "','MM/DD/YY'), " + "msg_time='" + time + "'" + "where record_id = '" + record_id + "'";
                    /*"UPDATE ATM_EDC_RECON SET counters_print_flag ='C' where record_id = '" + record_id +"'" ;*/
                
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  UpdateEJ_Replinishment_DISP_Clear Code 004, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="teriminal_id_16"></param>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="clear_counter_flag"></param>
        /// <param name="cash_disp_cas1"></param>
        /// <param name="cash_disp_cas2"></param>
        /// <param name="cash_disp_cas3"></param>
        /// <param name="cash_disp_cas4"></param>
        /// <param name="record_type"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool InsertEJ_Replinishment_DISP_Add(string teriminal_id_16, string date, string time,
          char clear_counter_flag, string cash_disp_cas1, string cash_disp_cas2, string cash_disp_cas3, string cash_disp_cas4,
          string record_type,string filename, int iFile_Number,string global_term_id, string global_date)
        {
            try
            {
                //string bin = "589206";

                string fdate = global_date; ;
                string f_termid = global_term_id.Substring(3, 4);
                /*
                if (filename.Length > 8)
                {
                    fdate = filename.Substring(17, 8);
                    f_termid = filename.Substring(12, 4);
                    
                }*/
                string record_id = "";

                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT lpad(to_char(recnum_seq.nextval),7,'0' ) FROM DUAL";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();
                record_id = (string)dr.GetValue(0);

                oraCom.CommandText = "INSERT INTO ATM_EDC_RECON(bin,terminal_id,terminal_id_4digit,msg_date,msg_time," +
                "counters_print_flag,record_date,record_id,cash_disp_cas1,cash_disp_cas2,cash_disp_cas3, cash_disp_cas4, record_type,file_number)" +
                " VALUES ( '" + bank_bin + "','" + teriminal_id_16 + "', '" + f_termid + "', " + "to_date('" + date + "','MM/DD/YY'),'" + 
                time + "', '" + clear_counter_flag + "', " + "to_date('" + fdate + "','yyyymmdd'),'" +
                 record_id + "', '" + cash_disp_cas1 + "', '" + cash_disp_cas2 + "', '" + cash_disp_cas3 + "', '" + cash_disp_cas4 + "', '"
                 + record_type + "',"+iFile_Number+")";
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Replinishment_DISP_Add Code 022, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                
                return false;
            }
            finally
            {
                oraCon.Close();
            }

            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="teriminal_id_16"></param>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="last_cleared_date"></param>
        /// <param name="last_cleared_time"></param>
        /// <param name="clear_counter_flag"></param>
        /// <param name="cash_disp_cas1"></param>
        /// <param name="cash_disp_cas2"></param>
        /// <param name="cash_disp_cas3"></param>
        /// <param name="cash_disp_cas4"></param>
        /// <param name="cash_rem_cas1"></param>
        /// <param name="cash_rem_cas2"></param>
        /// <param name="cash_rem_cas3"></param>
        /// <param name="cash_rem_cas4"></param>
        /// <param name="cash_tot_cas1"></param>
        /// <param name="cash_tot_cas2"></param>
        /// <param name="cash_tot_cas3"></param>
        /// <param name="cash_tot_cas4"></param>
        /// <param name="record_type"></param>
        /// <param name="filename"></param>
        /// <param name="record_id_in"></param>
        /// <returns></returns>

        public bool InsertEJ_Replinishment_DISP_Print(string teriminal_id_16, string date, string time,
            string last_cleared_date, string last_cleared_time, char clear_counter_flag,
            string cash_disp_cas1,string cash_disp_cas2,string cash_disp_cas3,string cash_disp_cas4,
            string cash_rem_cas1,string cash_rem_cas2,string cash_rem_cas3,string cash_rem_cas4,
            string cash_rej_cas1, string cash_rej_cas2, string cash_rej_cas3, string cash_rej_cas4,
            string cash_tot_cas1,string cash_tot_cas2,string cash_tot_cas3,string cash_tot_cas4,
            string cash_cur_cas1, string cash_cur_cas2, string cash_cur_cas3, string cash_cur_cas4,
            string tot_disp_a, string tot_disp_b, string tot_disp_c, string tot_disp_d,
            string tot_disp_amt_sar, string tot_disp_amt_usd,
            string record_type, string filename, int iFile_Number, out string record_id_in, string global_term_id, string global_date)
        {
            try
            {
               // string bin = "589206";//"588850";

                string fdate = global_date; ;
                string f_termid = global_term_id.Substring(3, 4);
                /*
                if (filename.Length > 8)
                {
                    fdate = filename.Substring(17, 8);
                    f_termid = filename.Substring(12, 4);
                    
                }*/
                string record_id = "";
                          
                
                oraCom = new OracleCommand();
                oraCom.CommandText = "SELECT lpad(to_char(recnum_seq.nextval),7,'0' ) FROM DUAL";

                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                OracleDataReader dr = oraCom.ExecuteReader();
                dr.Read();
                record_id = (string ) dr.GetValue(0);

                record_id_in = "";
                oraCom.CommandText = "INSERT INTO ATM_EDC_RECON(bin,terminal_id,terminal_id_4digit,msg_date,msg_time,prev_cleared_date,prev_cleared_time," +
                "counters_print_flag,record_date,record_id,cash_disp_cas1,cash_disp_cas2,cash_disp_cas3,cash_disp_cas4,"+
                "cash_rej_cas1,cash_rej_cas2,cash_rej_cas3,cash_rej_cas4, cash_rem_cas1, cash_rem_cas2,cash_rem_cas3,cash_rem_cas4,cash_tot_cas1," +
                "cash_tot_cas2,cash_tot_cas3,cash_tot_cas4,cash_cur_cas1,cash_cur_cas2,cash_cur_cas3,cash_cur_cas4,"+
                "tot_disp_a,tot_disp_b,tot_disp_c,tot_disp_d,tot_disp_amt_sar,tot_disp_amt_usd," +
                "record_type,file_number)" +
                " VALUES ( '" + bank_bin + "','" + teriminal_id_16 + "', '" + f_termid + "', " + "to_date('" + date + "','MM/DD/YY'),'" +  time + "', " + "to_date('" + last_cleared_date + "','MM/DD/YY'),'" +
                 last_cleared_time + "', '" + clear_counter_flag + "', " + "to_date('" + fdate + "','yyyymmdd'),'" +
                 record_id + "', '" + cash_disp_cas1.Trim() + "', '" + cash_disp_cas2.Trim() + "', '" + cash_disp_cas3.Trim() + "', '" + cash_disp_cas4.Trim() + "', '"
                  + cash_rej_cas1.Trim() + "', '" + cash_rej_cas2.Trim() + "', '" + cash_rej_cas3.Trim() + "', '" + cash_rej_cas4.Trim() + "', '"
                 + cash_rem_cas1.Trim() + "', '" + cash_rem_cas2.Trim() + "', '" + cash_rem_cas3.Trim() + "', '" + cash_rem_cas4.Trim() + "', '"
                 + cash_tot_cas1.Trim() + "', '" + cash_tot_cas2.Trim() + "', '" + cash_tot_cas3.Trim() + "', '" + cash_tot_cas4.Trim() + "', '"
                 + cash_cur_cas1.Trim() + "', '" + cash_cur_cas2.Trim() + "', '" + cash_cur_cas3.Trim() + "', '" + cash_cur_cas4.Trim() + "', '"
                 + tot_disp_a.Trim() + "', '" + tot_disp_b.Trim() + "', '" + tot_disp_c.Trim() + "', '" + tot_disp_d.Trim() + "', '"
                 + tot_disp_amt_sar.Trim() + "', '" + tot_disp_amt_usd.Trim() + "', '" 
                 + record_type +  "'," +iFile_Number+")";
                oraCom.Connection = oraCon;
                if (oraCon.State != ConnectionState.Open)
                    oraCon.Open();

                if (oraCom.ExecuteNonQuery() > 0)
                {
                    record_id_in = record_id;
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertEJ_Replinishment_DISP_Print Code 003, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                record_id_in = "";
                return false;
            }
            finally
            {
                oraCon.Close();
            }
                 
         }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="dev_status"></param>
        /// <returns></returns>

        public ArrayList GetStatusData_NCR (string deviceID, string dev_status,string strMStatus)
        {
            ArrayList arrResult = new ArrayList();
            int sflag = -1;
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandText = "EJ_Status_Retrieve.GetEJStatusNCR";
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;

                /*P_IN_DieBoldStatusMsg IN  VARCHAR2,
                       P_Out_StatusDesc      OUT VARCHAR2,
                       P_Out_IntClaim        OUT NUMBER,
                       P_Out_StatusMsgLen    OUT INTEGER*/
                oraCom.Parameters.Add("P_IN_NCRDevice", OracleType.VarChar, 200).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("P_IN_NCRDeviceStatus", OracleType.VarChar, 200).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("P_IN_NCRModuleStatus", OracleType.VarChar, 200).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("P_Out_StatusDesc", OracleType.VarChar,1000).Direction = ParameterDirection.Output;
                oraCom.Parameters.Add("P_Out_IntClaim", OracleType.Number).Direction = ParameterDirection.Output;

                oraCom.Parameters.Add("P_Out_StatusMsgLen", OracleType.Number).Direction = ParameterDirection.Output;


                oraCom.Parameters["P_IN_NCRDevice"].Value = deviceID;
                oraCom.Parameters["P_IN_NCRDeviceStatus"].Value = dev_status;
                oraCom.Parameters["P_IN_NCRModuleStatus"].Value = strMStatus;

                DataTable dt = new DataTable();
                OracleDataAdapter da = new OracleDataAdapter(oraCom);
                //da.SelectCommand = oraCom;
                da.Fill(dt);
                sflag = int.Parse(oraCom.Parameters["P_Out_IntClaim"].Value.ToString());
                arrResult.Add(sflag);
                arrResult.Add(oraCom.Parameters["P_Out_StatusDesc"].Value.ToString());

            }
            catch (Exception ex)
            {
                objLogger.LogMsg ("Error Code 008: Function:  GetStatusData_NCR, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText +
                    "deviceID= " + deviceID + ", status=" + dev_status);
                arrResult.Add(sflag);
                arrResult.Add("Exception in Package calling, check EJ logs for details");
            }
            return arrResult;
        }


        
        
       /* public ArrayList GetTransactionFromMasterTran(int trace, string ATMID, string txnDate)
        {
            DataTable dt = new DataTable();
            ArrayList Result = new ArrayList();
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandText = "EJ_UPLOAD_ANB.sp_GetTxnFrom_MasterTran";
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;

                oraCom.Parameters.Add("p_trace", OracleType.Number).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("p_date", OracleType.VarChar, 10).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("p_terminal", OracleType.VarChar, 16).Direction = ParameterDirection.Input;
                oraCom.Parameters.Add("p_pan", OracleType.VarChar,20).Direction = ParameterDirection.Output;
                oraCom.Parameters.Add("p_time", OracleType.VarChar,10).Direction = ParameterDirection.Output;


                oraCom.Parameters["p_trace"].Value = trace;
                oraCom.Parameters["p_date"].Value = txnDate;
                oraCom.Parameters["p_terminal"].Value = ATMID;
                OracleDataAdapter da = new OracleDataAdapter(oraCom);
                da.Fill(dt);
                Result.Add(oraCom.Parameters["p_pan"].Value.ToString());
                Result.Add(oraCom.Parameters["p_time"].Value.ToString());
            }
            catch (Exception ex)
            {
                Result.Add("XXXX");
                Result.Add("00:00");
                objLogger.LogMsg("Error Code 008: Function:  GetTransactionFromMasterTran, Source:  " + ex.Source + " , Message:" + ex.Message 
                    + ", SQL: " + oraCom.CommandText + ", Parameters: " + trace + "," + ATMID + "," + txnDate);
            }
            return Result;
        }*/

        public DataTable GetATMCassettes(string ATM_ID)
        {
            DataTable dtRes = new DataTable();
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandText = "Select * from ATM_GROUP_TBL Where GID = (Select ATM_Group From ATM_DATA_TABLE Where ATM_Number  ='" + ATM_ID + "')";
                oraCom.Connection = oraCon;
                OracleDataAdapter da = new OracleDataAdapter(oraCom);
                //DataSet ds = new DataSet();
                //OracleDataReader dr = new OracleDataReader();

                da.Fill(dtRes);
            }
            catch (Exception ex)
            {
                dtRes.Rows.Clear();
                objLogger.LogMsg("Error Code 008: Function:  GetATMCassettes, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
            }
            return dtRes;
        }
       
        public bool Update_EJ_PROCESS_LOG(string strRecordDate,  int iTotalProcessedRecords, int iTotalRejectedRecords, int iEPD_CODE)
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.Connection = oraCon;

                oraCom.CommandText = "Update EJ_PROCESS_LOG Set PROC_END_TIME ='" + DateTime.Now.Hour.ToString().PadLeft(2,'0') + ":" + DateTime.Now.Minute.ToString().PadLeft(2,'0') +
                    "',PROCESSING_STATUS = '002', RECORDS_LOADED =" + iTotalProcessedRecords +
                    ",RECORDS_REJECTED = " + iTotalRejectedRecords + " Where (RECORD_DATE = '" + strRecordDate + "') AND EPD_CODE = "+iEPD_CODE.ToString() ;

                //objLogger.LogMsg(oraCom.CommandText);
                if(oraCon.State != ConnectionState.Open)
                    oraCon.Open();
                if (oraCom.ExecuteNonQuery() > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 009: Function:  Update_EJ_PROCESS_LOG, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                return false;
            }
        }

        //Start EJ application JOb
        public bool StartApplicationJob( string reconDate)
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;
                //oraCom.CommandText = "ASR.job_util.begin_job_processing";
                oraCom.CommandText = "job_util.begin_job_processing";
                oraCom.Parameters.Add(new OracleParameter("p_current_uid", "0"));
                oraCom.Parameters.Add(new OracleParameter("p_job_name", "EJ_NCR_LOAD"));
                oraCom.Parameters.Add(new OracleParameter("p_file_name", ""));
                oraCom.Parameters.Add(new OracleParameter("p_rc_code", "1"));
                oraCom.Parameters.Add(new OracleParameter("p_re_code", "0"));
                oraCom.Parameters.Add(new OracleParameter("p_recon_date", reconDate));
                oraCom.Parameters.Add(new OracleParameter("p_start_method", "Scheduled"));
                oraCon.Open();
                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  InsertIntoApplicationJob Code 015, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }

        }

        //Added to End the process of EJ in the Application Job Table and report the error
        public bool EndApplicationJobWithError(string strErrMsg)
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;
                //oraCom.CommandText = "ASR.job_util.end_job_processing";
                oraCom.CommandText = "job_util.end_job_processing";

                oraCom.Parameters.Add(new OracleParameter("p_job_name", "EJ_NCR_LOAD"));
                oraCom.Parameters.Add(new OracleParameter("p_response", strErrMsg));
                oraCon.Open();
                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 018: Function:  EndApplicationJobWithError, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
        }

        

        //Added to End the process of EJ in the Application Job Table
        public bool EndApplicationJob()
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;
                //oraCom.CommandText = "ASR.job_util.end_job_processing";
                oraCom.CommandText = "job_util.end_job_processing";
               
                oraCom.Parameters.Add(new OracleParameter("p_job_name", "EJ_NCR_LOAD"));
                oraCom.Parameters.Add(new OracleParameter("p_response", "Successful"));
                oraCon.Open();
                if (oraCom.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else return false;

            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 008: Function:  EndApplicationJob, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                oraCon.Close();
                return false;
            }
            finally
            {
                oraCon.Close();
            }
        }

        

        public bool Update_ATM_Table()
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandType = CommandType.StoredProcedure;
                oraCom.Connection = oraCon;
                oraCom.CommandText = "EJ_UPLOAD_ANB.UPDATE_ATM_TABLE";
                oraCom.Parameters.Add("p_res", OracleType.Number).Direction = ParameterDirection.Output;                
                oraCon.Open();
                oraCom.ExecuteNonQuery();
                oraCon.Close();
                
                return true;
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 008: Function:  Update_ATM_Table, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                return false;
            }
        }

  
       
      /*  public int GetfileNumber(string fileName)
        {
            DataTable dt = new DataTable();
            try
            {
                oraCom = new OracleCommand();
                oraCom.Connection = oraCon;
                string file_id = fileName.Replace("Ascii_", "");
                oraCom.CommandText = "Select File_Number From EJ_PROCESS_LOG Where trim(file_id)=trim('" + file_id + "')";
                oraDa.SelectCommand = oraCom;
                oraDa.Fill(dt);
                return int.Parse(dt.Rows[0][0].ToString());
            }
            catch (Exception ex)
            {
                dt.Rows.Clear();
                objLogger.LogMsg("Error Code 008: Function:  GetfilesNames, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                return 0;
            }
        }*/

        public string GetNextFileID()
        {
            try
            {
                oraCom = new OracleCommand();
                oraCom.CommandText = "Select ej_file_number.nextval from dual";
                oraCom.Connection = oraCon;
                DataTable dt = new DataTable();
                OracleDataAdapter da = new OracleDataAdapter(oraCom);
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
                else
                    return "";
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Function:  GetNextFileID Code 001, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                return "";
            }
        }

        public bool CheckFileProcessed(string fileName)
        {
            try
            {
                oraCom.CommandText = "Select FILE_NAME from ATM_EDC_HISTORY Where FILE_NAME = '" + fileName.Trim() + "' ";
                oraCom.Connection = oraCon;
                DataTable dt = new DataTable();
                OracleDataAdapter da = new OracleDataAdapter(oraCom);
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error Code 008: Function:  CheckFileProcessed, Source:  " + ex.Source + " , Message:" + ex.Message + ", SQL: " + oraCom.CommandText);
                return false;
            }
        }

      
      
    }
}
