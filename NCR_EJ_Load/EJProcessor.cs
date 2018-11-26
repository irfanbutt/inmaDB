using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using System.IO;
using System.Text.RegularExpressions;

//change
namespace NCR_EJ_Load
{
    class EJProcessor
    {

        Logger objLogger;
        DBProcessor objDB;
        /*int NCR_All_TxnsCounter = 0;
        int NCR_Accepted_TxnsCounter = 0;
        int NCR_Rejected_TxnsCounter = 0;*/
        //int NCR_All_StatusesCounter = 0;
        /*char txntemp[256], sttemp[256], date1[7],date2[10],date3[10],pcode[9],
         * amt[14],time1[7],time2[7],luno[10],txntype[3],trace[11],termid[17],
         * disp_denom[13],mis[128],fromacctno[33],toacctno[21],pan[20];*/
        public string strThreshold, strMissingBulk, strNoOfTxn, strNoOfStatus, strTermID, strFdate;
        public string cass_type_1_denom, cass_type_2_denom, cass_type_3_denom, cass_type_4_denom;
        public string cass_type_1_curr, cass_type_2_curr, cass_type_3_curr, cass_type_4_curr, convert_file, global_bank_bin;
        public int file_number;
        //public string cass_type_1_curr, cass_type_2_curr, cass_type_3_curr, cass_type_4_curr;

        // Coin Area Starts

        public static string patternCoin, patternNotes;

        public static string[] CoinBINS;

        // Con Area End

        public EJProcessor(string ORA_CON_STRING)
        {
            try
            {
                objLogger = new Logger();
                objDB = new DBProcessor(ORA_CON_STRING, global_bank_bin);
                objDB.OpenConnection();
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Error: Constructor EJProcessor: , ex Source: " + ex.Source + ", Message: " + ex.Message);
            }
        }

        public int ProcessEJFile(string filePath, string fileName)
        {
            if (File.Exists(filePath + fileName))
            {
                /***** Checking the file is already proccessed or not with History table */
                if ( objDB.CheckFileProcessed(fileName) == true)
                {
                    Console.WriteLine("Error: File already processed : "+ fileName);
                    objLogger.LogMsg("Error: File already processed : "+ fileName);
                    return -1;
                }
                /***** End Checking the file is already proccessed or not with History table */

                
                
                 // string[] Splitter = { "____" };
                //UMER_MOD 141208 starts
                int transactionCounter = 0;
                //UMER_MOD 141208 ends
                string[] strLines= File.ReadAllLines(filePath + fileName);
                //Coint Starts
                //string[,] strTransactions =new string[strLines.Length,10];
                string[,] strTransactions = new string[strLines.Length, 12];
                // Coin Ends
                //UMER_MOD 150113 starts
                string[,] strDispDenomsWithStan = new string[strLines.Length, 2];
                int iDispDenomsWithStan = 0;
                int iDispDenomAmt =0;
                bool bConsiderNotesPresented = false;
                
                //UMER_MOD 150113 starts

                string[] strNonTransactions=new string[strLines.Length];
                //string[,] strReplRecords = new string[(strLines.Length >= 400) ? ((strLines.Length / 400 )+1) : (strLines.Length+1), 400];
                string[,] strReplRecords = new string[strLines.Length , 400];
                //UMER_MOD 140521 starts 
                //string[] strStatusRecord = new string[strLines.Length];
                string[,] strStatusRecord = new string[strLines.Length,2];
                //UMER_MOD 140521 ends 
                //UMER_MOD 141203 starts
                string[,] strTrnxTraceDateTime = new string[strLines.Length,3];
                //UMER_MOD 141203 ends

                ConfigReader objConfReader = new ConfigReader();
                ArrayList objArr_Config = new ArrayList();
                objArr_Config= objConfReader.ReadParsingConfig();
                string StartSentinel = (string)objArr_Config[0];
                string EndSentinel = (string)objArr_Config[1];


                string strGlobalTermID = "";
                string strGlobalFileDate = "";
                
                int nonTransactionCounter = 0;
                int statusCounter = 0;
                int replCounter = 0;

                //UMER_MOD 140521 starts 
                //Added for the previous trace,date and time for the statuses
                string[] prevTxnTraces = new string[strLines.Length];
                //string prevTxnDate = "";
                //string prevTxnTime = "";
                //UMER_MOD 140521 ends 
             
                /* variables required for Transaction Processing starts*/
                string date1, date2, date3;
                string time1, time2, time3;
                string pcode, amt, luno, txntype, trace, termid, disp_denom, fromacctno, toacctno, pan, txn_cur, prev_trace,host_resp;
                /* variables required for Transaction Processing ends*/

                /* variables required for Status Processing starts*/
                string term_id, term_trace, atm_type, status_desc, stat_date, stat_time, rec_date,prev_txn_trace,prev_stat_date,prev_stat_time;
                string device, device_status, status_severity;
                string[] strStatusRecordSegs = new string[2];
                string strMStatus = "";
                /* variables required for Status Processing ends*/


                /* variables required for Repl Processing starts*/
                /*string strCassette_A = "";
                string strCassette_B = "";
                string strCassette_C = "";
                string strCassette_D = "";*/

                string strCass_Rej_A = "";
                string strCass_Rej_B = "";
                string strCass_Rej_C = "";
                string strCass_Rej_D = "";

                string strCass_Rem_A = "";
                string strCass_Rem_B = "";
                string strCass_Rem_C = "";
                string strCass_Rem_D = "";

                string strCass_Disp_A = "";
                string strCass_Disp_B = "";
                string strCass_Disp_C = "";
                string strCass_Disp_D = "";

                string strCass_Tot_A = "";
                string strCass_Tot_B = "";
                string strCass_Tot_C = "";
                string strCass_Tot_D = "";

                string strDispLastClearedDate = "";
                string strDispLastClearedTime = "";

                string strBNALastClearedDate = "";
                string strBNALastClearedTime = "";

                int iCashDep_50 = 0;
                int iCashDep_100 = 0;
                int iCashDep_200 = 0;
                int iCashDep_500 = 0;

                int iMinusOneRecord = 0;

                int iDepRet_50 = 0;
                int iDepRet_100 = 0;
                int iDepRet_200 = 0;
                int iDepRet_500 = 0;

                //MOH - 16-02-2015 - Starts - Deposit Rejects
                int iDepRej_50 = 0;
                int iDepRej_100 = 0;
                int iDepRej_200 = 0;
                int iDepRej_500 = 0;
                //MOH - 16-02-2015 - Ends - Deposit Rejects
                char clear_counter_flag = 'P';
                char clear_counter_flag_bna = 'P';
                string strMsgDate = "";
                string strMsgTime = "";
                string strRecordID = "";
                string strRecordID_BNA = "";

                string strTotalAmountSAR="0";
                string strTotalAmountUSD="0";
                string strTotalDispCass1="0", strTotalDispCass2="0", strTotalDispCass3="0", strTotalDispCass4 ="0";
                string strNotesPresented ="";
                /* variables required for Repl Processing ends*/
                // Segregate transactions and other (status/repl) records 
                try
                {
                    strGlobalFileDate = fileName.Substring(fileName.IndexOf('_') + 1, 8);
                }
                catch (Exception fileNameEx)
                {
                    objLogger.LogMsg("There was exception in file name, could not get date value " + fileName + " Exception is : " + fileNameEx.Message);
                    strGlobalFileDate = "20010101";
                }
                for (int iCount = 0; iCount < strLines.Length; iCount++)
                    {
                        
                        try
                        {
                         
                        if (convert_file == "TRUE")
                        {
                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", strLines[iCount]);
                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", Environment.NewLine);
                        }

                        /*UMER_MOD 20140402 start sentinel for transaction changed to have only # in line */
                        //if (strLines[iCount].Contains(StartSentinel) && (strLines[iCount].Split('/').Length > 2))

                        if (strLines[iCount].Contains("NOTES PRESENTED"))
                        {
                            strNotesPresented = strLines[iCount];
                           
                            
                        }

                        //UMER_MOD 150113 starts
                            
                        if (
                            strLines[iCount].Contains("STAN:")
                            && strLines[iCount ].Contains("C1")
                            && strLines[iCount ].Contains("C4"))
                        {
                            strDispDenomsWithStan[iDispDenomsWithStan, 0] = strLines[iCount].Substring(5, 4);//its stan
                            strDispDenomsWithStan[iDispDenomsWithStan, 1] = strLines[iCount].Substring(9, 16);//its denoms
                            iDispDenomsWithStan++;  
                        }

                        //UMER_MOD 150113 ends

                        if (strLines[iCount].Contains(StartSentinel) && (strLines[iCount].Count(c => c == '|') == 3))
                        {
                            //its start of transaction
                            strTransactions[transactionCounter, 0] = strLines[iCount].Substring(0,strLines[iCount].Length-1);
                            int iRecCount = 1;
                            
                            if (strGlobalTermID == "" && iRecCount == 1)
                            {
                                string[] RecordSplitted = strLines[iCount].Split('|');
                                RecordSplitted = RecordSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                strGlobalTermID = RecordSplitted[2].Trim();
                                objDB.Insert_ATM_If_Missing(strGlobalTermID);
                            }
                            
                            iCount++;

                            while ((!strLines[iCount].Contains(EndSentinel)) && (iCount < strLines.Length))
                            {
                                //objLogger.LogMsg("tempx: There No of txn are:  " + transactionCounter);
                                /* removing control characters and preparing an ascii output starts*/
                                //strLines[iCount] = RemoveControlCharsFromString(strLines[iCount]);
                                if (convert_file == "TRUE")
                                {
                                    File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", strLines[iCount]);
                                    File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", Environment.NewLine);
                                }
                                /* removing control characters and preparing an ascii output ends*/

                                strTransactions[transactionCounter, iRecCount] = strLines[iCount];
                                iRecCount++;
                                iCount++;
                            }
                       
                            strTransactions[transactionCounter, iRecCount] = strLines[iCount];
                            
                            /* to add end sentinel to converted File */

                            // Coin Area Starts          @"\d{1}[.]\d{2}[X]"   @"\d{2}[X]"
                            // string Deonomination = "";

                            Regex rgxCoin = new Regex(patternCoin);
                            Regex rgxNotes = new Regex(patternNotes);

                            if (strLines[iCount + 1].Contains("ETT") ) //|| rgxNotes.IsMatch(strLines[iCount]))
                            {
                                iRecCount++;
                                iCount++;
                                strTransactions[transactionCounter, iRecCount] = strLines[iCount]; // ETT

                                if (rgxCoin.IsMatch(strLines[iCount + 1]))
                                {
                                    iRecCount++;
                                    iCount++;
                                    strTransactions[transactionCounter, iRecCount] = strLines[iCount];
                                }
                            }
                            // Coin Area Ends
                            else
                            {
                                iRecCount++;
                              strTransactions[transactionCounter, iRecCount] = strNotesPresented; //Cash Deonomination before switch response
                            
                            }

                            
                          
                            
                            if (convert_file == "TRUE")
                            {
                                File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", strLines[iCount]);
                                File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", Environment.NewLine);
                            }
                            transactionCounter++;
                        }
                        else
                        {
                            //non transactions come here...
                            if (strLines[iCount].Split('*').Length > 4 /*&& strLines[iCount].Split(',').Length > 2*/)
                            {
                                //strLines[iCount] = RemoveControlCharsFromString(strLines[iCount]);
                                if ((strLines[iCount].IndexOf('*', strLines[iCount].IndexOf('*', 0) + 1) == (strLines[iCount].IndexOf('*', 0) + 5)) && (strLines[iCount].Contains("M") && strLines[iCount].Contains("R")))
                                {
                                    // its for sure a status //                               
                                    if (iCount > 0)
                                    {
                                        int previoiusIndex = 0;
                                        if (strLines[iCount - 1].Contains("PRESENTER ERROR") && iCount > 1)
                                        {
                                            previoiusIndex = iCount - 2;
                                        }
                                        else
                                        {
                                            previoiusIndex = iCount - 1;
                                        }
                                        if (strLines[previoiusIndex].Split('*').Length > 4 && strLines[previoiusIndex].Split('/').Length > 2)
                                        {
                                            // we have time and date in previous line for this status
                                            strLines[previoiusIndex] = RemoveControlCharsFromString(strLines[previoiusIndex]);
                                            strStatusRecord[statusCounter, 0] = strLines[previoiusIndex] + "<-------->" + strLines[iCount];
                                            statusCounter++;
                                        }
                                        else
                                        {
                                            strStatusRecord[statusCounter, 0] = "*000*01/01/2001*01:01*" + "<-------->" + strLines[iCount];
                                            statusCounter++;
                                        }
                                        //UMER_MOD 140521 starts
                                        if (transactionCounter > 0 && statusCounter > 0)
                                            strStatusRecord[statusCounter - 1, 1] = (transactionCounter - 1).ToString();
                                        //UMER_MOD 140521 ends
                                    }
                                }
                            }//if (strLines[iCount].Split('*').Length > 4 && strLines[iCount].Split(',').Length > 2)


//====================================================================================================================================================

                            /*non transactions come here... Commented for SAIB  By MOHANA
                            if (strLines[iCount].Split('*').Length > 4 || (strLines[iCount].Contains("M-") || strLines[iCount].Contains("R-")))
                            {
                                    // its for sure a status //                               
                                int previousiCount = iCount; 
                                            int sCount = 0;
                                            Console.WriteLine("Strlines lenght:" + strLines[iCount].Split('*').Length);
                                            strStatusRecord[statusCounter,sCount] =  strLines[iCount];

                                            while ((iCount < strLines.Length) && (!strLines[iCount].Contains("TRANSACTION END")))
                                            {
                                                if (strLines[iCount].Contains("M-") || strLines[iCount].Contains("R-"))
                                                {
                                                    sCount++;
                                                    strStatusRecord[statusCounter, sCount] = strLines[iCount];
                                                }
                                                iCount++;
                                            }
                                            
                                            statusCounter++;
                                            iCount = previousiCount;
                                            //UMER_MOD 140521 starts
                                            //if (transactionCounter > 0 && statusCounter > 0)
                                                //strStatusRecord[statusCounter - 1, 1] = (transactionCounter - 1).ToString();
                                            //UMER_MOD 140521 ends                                     
           
                            }if (strLines[iCount].Split('*').Length > 2) Commented for SAIB BY MOHANA */
//========================================================================================================================================
                            else
                            {
                                if (strLines[iCount].Contains("SUPERVISOR MODE ENTRY"))
                                {

                                    int iReplCount = 0;
                                    do
                                    {
                                      
                                        //strLines[iCount] = RemoveControlCharsFromString(strLines[iCount]);
                                        strReplRecords[replCounter, iReplCount] = strLines[iCount];
                                        if (convert_file == "TRUE")
                                        {
                                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", strLines[iCount]);
                                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", Environment.NewLine);
                                        }
                                        iReplCount++;
                                        iCount++;

                                  

                                    } while (iCount < (strLines.Length - 1) && 
                                        !(strLines[iCount].Contains("SUPERVISOR MODE EXIT")) &&
                                        !(strLines[iCount].Contains("OUT OF SERVICE")) &&
                                        !(strLines[iCount].Contains("POWER-UP/RESET")) &&
                                        !(strLines[iCount].Contains("TRANSACTION START")) 
                                        );
                                    if (strLines[iCount].Contains("SUPERVISOR MODE EXIT"))
                                    {
                                        if (convert_file == "TRUE")
                                        {
                                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", strLines[iCount]);
                                            File.AppendAllText(filePath + fileName + "_ConvertedFile.txt", Environment.NewLine);
                                        }
                                        strReplRecords[replCounter, iReplCount] = strLines[iCount];
                                    }

                                    replCounter++;
                                }

                                strNonTransactions[nonTransactionCounter] = strLines[iCount];
                                nonTransactionCounter++;
                            }
                        }

                    }//try
                            catch (Exception ParsingEx)
                        {
                            objLogger.LogMsg("There was a parsing error for line " + strLines[iCount]);
                            objLogger.LogMsg("The exception is : " + ParsingEx.Message);

                        }
                
                }//for

                //UMER_MOD 140611 starts
                // in cases where there was no transaction, the global terminal id should be taken from file name 
                if (strGlobalTermID == "")
                {
                    try
                    {
                        //int index_underscore = 0;
                        strGlobalTermID = fileName.Substring(0, fileName.IndexOf('_'));
                    }
                    catch (Exception fileNameEx)
                    {
                        objLogger.LogMsg("There was exception in file name, could not get terminal id from file, where there is no terminal id even in file " + fileName + " Exception is : " + fileNameEx.Message);
                        strGlobalTermID = "0000000000000000";
                    }
                }
                //UMER_MOD 140611 ends
                
                
                /* start processing all transactions */
                //prevTxnTraces = null;
                //prevTxnTraces = new string[transactionCounter];
                strMissingBulk = "101";
                prev_trace = "";
                string temp_amount="0";
              //  objLogger.LogMsg("temp: start processing txns");
                for (int iCount = 0; iCount < transactionCounter; iCount++)
                {
                   
                    try
                    {
                       

                    if ( strTransactions[iCount,0].Contains('/')  /*&& strTransactions[iCount, 2].Substring(0, 2) == "10"*/)
                    {
                        
                        date1 = date2 = date3 = "";
                        time1 = time2 = time3 = "";
                        pcode= amt= luno= txntype= trace= termid= disp_denom= fromacctno= toacctno= pan= txn_cur =   "";
                        //30/12/13 becomes 123013
                        date2 = strTransactions[iCount, 0].Substring(3, 2) + strTransactions[iCount, 0].Substring(0, 2) + strTransactions[iCount, 0].Substring(6, 2);
                        
                        //6:52 becomes 0652
                        if (strTransactions[iCount, 0].Substring(11, 1) == ":")
                        {
                            if (strTransactions[iCount, 0].Substring(9, 1) == " ")
                            {
                                time2 = "0" + strTransactions[iCount, 0].Substring(10, 2) + strTransactions[iCount, 0].Substring(12, 2);
                            }
                            else
                            {
                                time2 = strTransactions[iCount, 0].Substring(9, 2) + strTransactions[iCount, 0].Substring(12, 2);
                                //UMER_MOD starts , time is not coming with leading zeroes, but its coming with zero in status thus
                                time2.PadLeft(4, '0');
                            }
                        }
                        else
                        {
                            time2 = "";
                        }

                        //UMER_MOD 20141203 -b starts
                        iMinusOneRecord=0;
                        if (strTransactions[iCount, 3].Contains("A/C"))
                        {
                         iMinusOneRecord =1;
                        }

                        pan = strTransactions[iCount, 3 - iMinusOneRecord].Substring(0, (strTransactions[iCount, 3 - iMinusOneRecord].Length - 1));
                        //UMER_MOD 20141203 starts
                        // toacctno = strTransactions[iCount, 4].Substring(0,(strTransactions[iCount,4].Length-1)).Substring(4);
                        fromacctno = strTransactions[iCount, 4 - iMinusOneRecord].Substring(0, (strTransactions[iCount, 4 - iMinusOneRecord].Length - 1)).Substring(4); //UMER_MOD 20141203 
                        toacctno = fromacctno; //UMER_MOD 20141203 
                        //UMER_MOD 20141203 ends
                        

                        //UMER_MOD 20141203 -b starts
                        string[] FirstRecordSplitted = strTransactions[iCount, 0].Split('|');
                        FirstRecordSplitted = FirstRecordSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        termid = FirstRecordSplitted[2];
                        trace = FirstRecordSplitted[1].Substring(4,(FirstRecordSplitted[1].Length-4));
                        //UMR_MOD 140521 starts
                        prevTxnTraces[iCount] = trace;
                        //UMR_MOD 140521 ends
                        string[] SecondRecordSplitted = strTransactions[iCount, 1].Split('|');
                        string[] ThirdRecordSplitted = strTransactions[iCount, 2].Split('|');
                        //if (ThirdRecordSplitted[0] != "")
                        if( SecondRecordSplitted[4].Length > 3)
                        {
                            host_resp = SecondRecordSplitted[4].Substring(3);  
                        }
                        else
                        {
                            host_resp = ThirdRecordSplitted[0];
                            
                        }
                        //UMR_MOD 140521 starts
                        //UMR_MOD 141203 -c starts
                        try
                        {
                            int.Parse(host_resp);
                        }
                        catch (Exception intEx)
                        {
                            host_resp = "09";
                        }
                        //UMR_MOD 141203 -c ends
                        //UMR_MOD 140521 ends

                        string[] NinethRecordSplitted = strTransactions[iCount, 8 - iMinusOneRecord].Split('|');
                        /* removing all empty records */
                        NinethRecordSplitted = NinethRecordSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        
                        //UMER_MOD 141203 starts
                        if (NinethRecordSplitted[0].Trim() == "TT:21")
                        {   
                           // pcode = "200001";
                            //its deposit
                            fromacctno = "";
                        }
                        else //if (NinethRecordSplitted[0].Trim() == "TT:01")
                        {
                            //pcode = "100100";
                            //its withdrawal
                            toacctno = "";
                        }
                       /* else
                        {
                            pcode = "000000";
                        }*/
                        pcode = NinethRecordSplitted[0].Trim().Substring(3, 2) + "0000";
                        //UMER_MOD 141203 else

                        amt = NinethRecordSplitted[1].Substring(3, NinethRecordSplitted[1].Length - 3).Trim();

                    
                      
                        //pcode = ForthRecordSplitted[0];
                        
                        //trace = ForthRecordSplitted[1];
                        /*if (pcode.Substring(0,1)=="7")
                            amt = strTransactions[iCount, 4].Trim();
                        else    
                        amt = ForthRecordSplitted[2];*/
                        /* code for bulk missing notifications starts */
                        if (iCount > 0 && prev_trace != "" & trace != "")
                        {
                            try
                            {
                                //trace = "abc";
                                if (Convert.ToInt32(trace) - Convert.ToInt32(prev_trace) > Convert.ToInt32(strThreshold))
                                {
                                    objLogger.LogMsg("Warning: There is a bulk missing in file: " + fileName + " from trace " + prev_trace + " to trace " + trace);
                                    strMissingBulk = "103";
                                }
                            }
                            catch (Exception ex)
                            {
                                objLogger.LogMsg("Err: There is an error while calculating bulk missing in file: " + fileName + " from trace " + prev_trace + " to trace " + trace + " exception is  " +ex.Message);
                                //strMissingBulk = "103";
                            }
                        }
                        
                        /*Commented for SAIB - BY MOHANA code for bulk missing notifications ends */
                        //int tempOut;
                        /*
                        if ((strTransactions[iCount, 4]) != null && (strTransactions[iCount, 4][0] >= '0' && strTransactions[iCount,4][0] <= '9'))
                        {
                            fromacctno = strTransactions[iCount, 4];
                        }
                        else
                        {
                            fromacctno ="00000000000000";
                        }

                        if (pcode.Substring(0, 2) != "10" && pcode.Substring(0, 2) != "30")
                        {
                            //its neither withdrawal nor inquiry thus get to_account details
                            if ((strTransactions[iCount, 5]!=null) && (strTransactions[iCount, 5][0] >= '0' && strTransactions[iCount, 5][0] <= '9'))
                            {
                                toacctno = strTransactions[iCount, 5];
                            }
                            else
                            {
                                toacctno = "00000000000000";
                            }

                        }
                        */

                         //UMER_MOD 150113 starts
                        //build denominatations from proper stans only
                   
                        disp_denom = "00000000";
                        bConsiderNotesPresented = true;
                            
                            iDispDenomsWithStan = 0;
                            while (iDispDenomsWithStan <= transactionCounter
                                /* && strDispDenomsWithStan[iDispDenomsWithStan, 0] != trace*/)
                            {
                                try
                                {
                                    if (strDispDenomsWithStan[iDispDenomsWithStan, 0]!=null &&
                                        strDispDenomsWithStan[iDispDenomsWithStan, 0].Trim() == trace.Trim())
                                    {
                                        disp_denom = strDispDenomsWithStan[iDispDenomsWithStan, 1].Substring(2, 2)
                                                     + strDispDenomsWithStan[iDispDenomsWithStan, 1].Substring(6, 2)
                                                     + strDispDenomsWithStan[iDispDenomsWithStan, 1].Substring(10, 2)
                                                     + strDispDenomsWithStan[iDispDenomsWithStan, 1].Substring(14, 2);

                                        iDispDenomsWithStan = transactionCounter + 1;//to break the loop
                                        bConsiderNotesPresented = false;
                                    }
                                    else
                                    {
                                        iDispDenomsWithStan++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    bConsiderNotesPresented = true;
                                    disp_denom = "00000000";
                                    objLogger.LogMsg("Error Calculating Dispense from STAN line : Exeption is " + ex.Message);   
                                }
                            }
                        
                            //UMER_MOD 150113 ends

                        //fromacctno = ""; //UMER_MOD 141203
                        if (NinethRecordSplitted[1].Substring(0, 3) == "SAR")
                        {
                            txn_cur = "682";
                        }
                        else if (NinethRecordSplitted[1].Substring(0, 3) == "USD")
                        {
                            txn_cur = "840";
                        }

                        else
                        {
                            //UMER_MOD 150113 starts
                            //put default currency value of SAR
                            //txn_cur = "682";
                            if (disp_denom.Substring(6, 2) == "00") //No dispense from USD Cassette
                                txn_cur = "682"; 
                            else
                                txn_cur = "840";
                            //Now calculate the amount as this transaction's amount was not present in correct place
                            try
                            {
                                iDispDenomAmt = 0;
                                iDispDenomAmt = Convert.ToInt32(disp_denom.Substring(0, 2)) * Convert.ToInt32(cass_type_1_denom)
                                                + Convert.ToInt32(disp_denom.Substring(2, 2)) * Convert.ToInt32(cass_type_2_denom)
                                                + Convert.ToInt32(disp_denom.Substring(4, 2)) * Convert.ToInt32(cass_type_3_denom)
                                                + Convert.ToInt32(disp_denom.Substring(6, 2)) * Convert.ToInt32(cass_type_4_denom);

                                amt = iDispDenomAmt.ToString() + ".00";
                            }
                            catch (Exception ex)
                            {
                                objLogger.LogMsg("Error Calculating amount from the STAN String : Exeption is " + ex.Message);   
                            }
                            //UMER_MOD 150113 ends

                            
                        }

                        string ETT_Code=""; // Only for Deposit;

                        if (pcode.Substring(0, 2) == "01")
                            //fields[15]+1,fields[15]+4,fields[15]+7,fields[15]+10,fields[15]+13 );
                        //01:54:41 NOTES PRESENTED 10,0,0,0
                        {
                            //UMER_MOD 150113 starts
                            //Only Need to take denoms from NOTES PRESENTED if not present with STAN as its not correctly coming
                            if (bConsiderNotesPresented)
                            {
                                // its withdrawal so get denoms
                                if (strTransactions[iCount, 9 - iMinusOneRecord].Contains("NOTES PRESENTED"))
                                {
                                    strTransactions[iCount, 9 - iMinusOneRecord] = strTransactions[iCount, 9 - iMinusOneRecord].Substring(strTransactions[iCount, 9 - iMinusOneRecord].IndexOf("NOTES PRESENTED") + 16);

                                }
                                else
                                {
                                    strTransactions[iCount, 9 - iMinusOneRecord] = "0,0,0,0";
                                }

                                string[] DenomSplitted = strTransactions[iCount, 9 - iMinusOneRecord].Split(',');

                                DenomSplitted = DenomSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();


                                disp_denom = DenomSplitted[0].PadLeft(2, '0') +
                                             DenomSplitted[1].PadLeft(2, '0') +
                                             DenomSplitted[2].PadLeft(2, '0') +
                                             DenomSplitted[3].PadLeft(2, '0') +
                                             "00";
                            }
                            //UMER_MOD 150113 ends
                            try
                                {
                                    strTotalDispCass1 = (Convert.ToInt32(strTotalDispCass1) + Convert.ToInt32(disp_denom.Substring(0, 2))).ToString();
                                    strTotalDispCass2 = (Convert.ToInt32(strTotalDispCass2) + Convert.ToInt32(disp_denom.Substring(2, 2))).ToString();
                                    strTotalDispCass3 = (Convert.ToInt32(strTotalDispCass3) + Convert.ToInt32(disp_denom.Substring(4, 2))).ToString();
                                    strTotalDispCass4 = (Convert.ToInt32(strTotalDispCass4) + Convert.ToInt32(disp_denom.Substring(6, 2))).ToString();
                                }
                                catch (Exception ex)
                                {
                                    objLogger.LogMsg("Error Calculating total dispense per cassettes : Exeption is " + ex.Message);   

                                }
                            //}
                            /*UMER_MOD 20140409 starts*/
                            /* addition calculation for total dispense amounts in this file  later used by repl entries*/
                               


                            temp_amount = amt.Replace(",","");
                            //UMER_MOD 20141208 starts
                            if (temp_amount == "")
                            {
                                temp_amount = "0"; 
                            }

                            //UMER_MOD 20141208 ends
                            try
                            {
                                if (txn_cur == "682")
                                {

                                    strTotalAmountSAR = (Convert.ToDouble(strTotalAmountSAR) + Convert.ToDouble(temp_amount)).ToString();

                                }
                                else if (txn_cur == "840")
                                {
                                    strTotalAmountUSD = (Convert.ToDouble(strTotalAmountUSD) + Convert.ToDouble(temp_amount)).ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                objLogger.LogMsg("Error Calculating total dispense per currency : Exeption is " + ex.Message);

                            }

                            /*UMER_MOD 20140409 ends*/ 
                            

                        }
                        else if (pcode.Substring(0, 2) == "21") // Deposit Area Start for both Coin & Cash
                        {

                            ETT_Code = Regex.Match(strTransactions[iCount, 9 - iMinusOneRecord], @"\d+").Value;
                            disp_denom = strTransactions[iCount, 10 - iMinusOneRecord];
                        }
                        //UMER_MOD 20141203 starts
                    

                        strTrnxTraceDateTime[iCount, 0] = trace;
                        strTrnxTraceDateTime[iCount, 1] = date2;
                        strTrnxTraceDateTime[iCount, 2] = time2;
                        //UMER_MOD 20141203 ends

                        /*UMER_MOD 20140521 starts*/ 
                        objDB.InsertEJ_Transaction(date1, date2, date3, time1, time2, time3, pcode, amt, luno,
                            txntype, trace, termid, disp_denom, fromacctno, toacctno, pan, txn_cur, host_resp, fileName, file_number, strGlobalTermID, strGlobalFileDate,ETT_Code);

                        /*objDB.InsertEJ_Transaction(date1, date2, date3, time1, time2, time3, pcode, temp_amount, luno,
                            txntype, trace, termid, disp_denom, fromacctno, toacctno, pan, txn_cur, host_resp, fileName, file_number, strGlobalTermID, strGlobalFileDate);*/
                        /*UMER_MOD 20140521 ends*/
                        prev_trace = trace;
                        //File.AppendAllText("d:\\tempfile2.txt",date1+ " date2-- " +date2+ " date3-- " +date3+ " time1-- " +time1+ "time2 -- " + time2+ " time3-- " + time3+ " pcode-- " +pcode+ " amt-- " + amt+ " luno-- " + luno+ "txntype -- " + txntype+ " trace-- " + trace+ "termid -- " + termid+ " denom-- " + disp_denom+ "from acct -- " + fromacctno+ "to acct -- " + toacctno+ "pan -- " + pan+ " txn_cur-- " + txn_cur );
                    
                    }//     if ( strTransactions[iCount,0].Contains('/')  && strTransactions[iCount, 2].Substring(0, 2) == "10")
                }// try
                catch(Exception TranEx)
                {
                        objLogger.LogMsg("There was exception in Tran Loading:  " + TranEx.Message );
                    
                }
                }//for (int iCount = 0; iCount < TransactionCounter ; iCount++)

                //objLogger.LogMsg("temp: There No of txn are:  " + transactionCounter);
                /* start processing all statuses */
                prev_stat_date = "";
                prev_stat_time = "";
                for (int iCount = 0; iCount < statusCounter; iCount++)
                {
                    /*string bin, term_id, term_trace, atm_type, status_desc, stat_date, stat_time, rec_date;
                    string device, device_status, status_severity;
                    string[] strStatusRecordSegs = new string[2];
                    string*/
                    strMStatus = "";
                    strStatusRecordSegs[0] = strStatusRecord[iCount, 0].Substring(0, strStatusRecord[iCount, 0].IndexOf("<-------->"));
                    strStatusRecordSegs[1] = strStatusRecord[iCount, 0].Substring(strStatusRecord[iCount, 0].IndexOf("<-------->") + 10, strStatusRecord[iCount, 0].Length - strStatusRecordSegs[0].Length - 10);

                    string[] statusSplitted = strStatusRecordSegs[0].Split('*');
                    statusSplitted = statusSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                    try
                    {

                        /*bin = "589206";*/
                        if (strGlobalTermID == "")
                        {
                            //strGlobalTermID = fileName.Substring(0, fileName.IndexOf('_')); //UMER_MOD 2014 dec 3
                            strGlobalTermID = fileName.Substring(3, 4);
                        }
                        term_id = strGlobalTermID;

                        if (statusSplitted[1] != "01/01/2001")
                        {
                            prev_stat_date = stat_date = statusSplitted[1];
                            prev_stat_time = stat_time = statusSplitted[2];

                        }
                        else
                        {
                            stat_date = prev_stat_date;
                            stat_time = prev_stat_time;

                        }
                        rec_date = System.DateTime.Now.Month.ToString() + "/" + System.DateTime.Now.Day.ToString() + "/" + System.DateTime.Now.Year.ToString();
                        atm_type = "NDC";


                        statusSplitted = strStatusRecordSegs[1].Split('*');
                        statusSplitted = statusSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        int iIgnoreEmptyRecord = 0;
                        if (statusSplitted[0].Trim().Length == 0)
                        {
                            iIgnoreEmptyRecord = 1;
                        }

                        term_trace = statusSplitted[0 + iIgnoreEmptyRecord];
                        device = statusSplitted[2 + iIgnoreEmptyRecord];
                        device_status = statusSplitted[3 + iIgnoreEmptyRecord];

                        //UMER_MOD 141203 starts
                        if (prev_stat_date == "")
                        {
                            for (int iSubCount = 0; iSubCount < transactionCounter; iSubCount++)
                            {
                                if (strTrnxTraceDateTime[iSubCount, 0].Trim() == term_trace.Trim())
                                {
                                    stat_date = strTrnxTraceDateTime[iSubCount, 1];
                                    stat_time = strTrnxTraceDateTime[iSubCount, 2];
                                    iSubCount = transactionCounter; //break the loop
                                }

                            }

                            if(stat_date =="" && transactionCounter>0)
                            {
                                stat_date = strTrnxTraceDateTime[0,1];
                                stat_time = strTrnxTraceDateTime[0,2];
                            }
                        }


                        //UMER_MOD 141203 ends

                        if (device_status.Contains("M-"))
                        {
                            strMStatus = device_status.Substring(device_status.IndexOf("M-"), 4);
                        }
                        /* get status details from the store procedure */

                        ArrayList arrRes = new ArrayList();
                        arrRes = objDB.GetStatusData_NCR(device, device_status.Substring(0, 1), strMStatus);
                        if (arrRes[1].ToString() != "")
                            status_desc = arrRes[1].ToString();
                        else
                            status_desc = device_status;

                        status_severity = arrRes[0].ToString();
                        //UMER_MOD 140519 starts
                        //updating the status trace to remove the leading zero as it is not coming with transaction data in ej and even in tlf
                        term_trace = term_trace.TrimStart(new Char[] { '0' });
                        //removing : from the time, as its not present in transaction time
                        if (stat_time.Contains(":"))
                            stat_time = stat_time.Remove(stat_time.IndexOf(":"), 1);

                        /*UMER_MOD 140521 starts*/
                        try
                        {
                            prev_txn_trace = prevTxnTraces[Convert.ToInt32(strStatusRecord[iCount, 1])];
                        }
                        catch (Exception ex)
                        {
                            objLogger.LogMsg("Error getting Previous transaction trace : Exeption is " + ex.Message + " - And prevTrace is " + strStatusRecord[iCount, 1]);
                            objLogger.LogMsg("at record number :" + iCount.ToString() + strStatusRecord[iCount, 0] + strStatusRecord[iCount, 1]);
                            prev_txn_trace = "";
                        }
                        /*UMER_MOD 140521 ends*/

                        //UMER_MOD 140519 ends
                        objDB.InsertEJ_Status(term_id, term_trace, atm_type, device + "*" + device_status, status_desc, stat_date, stat_time, status_severity, fileName, file_number, strGlobalTermID, strGlobalFileDate, prev_txn_trace);
                        //
                    }
                    catch (Exception StatusEx)
                    {
                        objLogger.LogMsg("There was exception in status Loading:  " + StatusEx.Message);
                    }

                }//for status processing

                //objLogger.LogMsg("temp2: There No of txn are:  " + transactionCounter);
//===============================================================================================================================================================================

                /* start processing all statuses Commented for SAIB - BY MOHANA
                for (int iCount = 0; iCount < statusCounter; iCount++)
                {
                    string bin, term_id, term_trace, atm_type, status_desc, stat_date, stat_time, rec_date;
                    string device, device_status, status_severity;
                    string[] strStatusRecordSegs = new string[2];
                    strMStatus = "";
                    //strStatusRecordSegs[0] = strStatusRecord[iCount,0].Substring(0, strStatusRecord[iCount,0].IndexOf("<-------->"));
                    //strStatusRecordSegs[1] = strStatusRecord[iCount,0].Substring(strStatusRecord[iCount,0].IndexOf("<-------->") + 10, strStatusRecord[iCount,0].Length - strStatusRecordSegs[0].Length - 10);

                    //string[] statusSplitted = strStatusRecordSegs[0].Split('*');
                    //statusSplitted = statusSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                    try
                    {

                        bin = "432328";
                        string[] statusSplitted;
                        stat_date = "";
                        stat_time = "";
                        if (strGlobalTermID == "")
                        {
                            strGlobalTermID = fileName.Substring(0, fileName.IndexOf('_'));
                        }
                        term_id = strGlobalTermID;
                        if ( strStatusRecord[iCount,0].Contains('/'))
                        {
                            statusSplitted = strStatusRecord[iCount,0].Split('*');
                            stat_date = statusSplitted[2];
                            stat_time = statusSplitted[3];
                        }
                        rec_date = System.DateTime.Now.Month.ToString() + "/" + System.DateTime.Now.Day.ToString() + "/" + System.DateTime.Now.Year.ToString();
                        atm_type = "NDC";

                        if (strStatusRecord[iCount, 1] == null)
                            continue;
                        
                            Console.WriteLine("strStatusRecord[iCount, 1] :" + strStatusRecord[iCount, 1]);
                            statusSplitted = strStatusRecord[iCount, 1].Split('*'); ;
                            statusSplitted = statusSplitted.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            int iIgnoreEmptyRecord = 0;
                            if (statusSplitted[0].Trim().Length == 0)
                            {
                                iIgnoreEmptyRecord = 1;
                            }

                            term_trace = statusSplitted[0 + iIgnoreEmptyRecord];
                            device = statusSplitted[2 + iIgnoreEmptyRecord];
                            device_status = statusSplitted[3 + iIgnoreEmptyRecord];

                            if (device_status.Contains("M-"))
                            {
                                strMStatus = device_status.Substring(device_status.IndexOf("M-"), 4);
                            }
                            // get status details from the store procedure 

                            ArrayList arrRes = new ArrayList();
                            arrRes = objDB.GetStatusData_NCR(device, device_status.Substring(0, 1), strMStatus);
                            if (arrRes[1].ToString() != "")
                                status_desc = arrRes[1].ToString();
                            else
                                status_desc = device_status;

                            status_severity = arrRes[0].ToString();
                            //UMER_MOD 140519 starts
                            //updating the status trace to remove the leading zero as it is not coming with transaction data in ej and even in tlf
                            term_trace = term_trace.TrimStart(new Char[] { '0' });
                            //removing : from the time, as its not present in transaction time
                            if (stat_time.Contains(":"))
                                stat_time = stat_time.Remove(stat_time.IndexOf(":"), 1);

                            //UMER_MOD 140521 starts
                            try
                            {
                                prev_txn_trace = prevTxnTraces[Convert.ToInt32(strStatusRecord[iCount, 1])];
                            }
                            catch (Exception ex)
                            {
                                objLogger.LogMsg("Error getting Previous transaction trace : Exeption is " + ex.Message + " - And prevTrace is " + strStatusRecord[iCount, 1]);
                                prev_txn_trace = "";
                            }
                            //UMER_MOD 140521 ends
                        
                        //UMER_MOD 140519 ends
                        objDB.InsertEJ_Status(term_id, term_trace, atm_type, device + "*" + device_status, status_desc, stat_date, stat_time, status_severity, fileName, file_number, strGlobalTermID, strGlobalFileDate,prev_txn_trace);
                        //
                    }
                    catch (Exception StatusEx)
                    {
                        objLogger.LogMsg("There was exception in status Loading:  " + StatusEx.Message);
                    }

                }//for status processing Commented for SAIB -  BY MOAHNA */

//======================================================================================================================================================================================================


                /* start processing all replinishment records */
                for (int iCount = 0; iCount <replCounter; iCount++)
                {
                    /* Data for Repl related cassettes counters */
                     /*strCassette_A = "";
                     strCassette_B = "";
                     strCassette_C = "";
                     strCassette_D = "";*/

                     strCass_Rej_A = "";
                     strCass_Rej_B = "";
                     strCass_Rej_C = "";
                     strCass_Rej_D = "";

                     strCass_Rem_A = "";
                     strCass_Rem_B = "";
                     strCass_Rem_C = "";
                     strCass_Rem_D = "";

                     strCass_Disp_A = "";
                     strCass_Disp_B = "";
                     strCass_Disp_C = "";
                     strCass_Disp_D = "";

                     strCass_Tot_A = "";
                     strCass_Tot_B = "";
                     strCass_Tot_C = "";
                     strCass_Tot_D = "";

                     strDispLastClearedDate = "";
                     strDispLastClearedTime = "";

                     strBNALastClearedDate = "";
                     strBNALastClearedTime = "";

                     iCashDep_50 = 0;
                     iCashDep_100 = 0;
                     iCashDep_200 = 0;
                     iCashDep_500 = 0;


                     iDepRet_50 = 0;
                     iDepRet_100 = 0;
                     iDepRet_200 = 0;
                     iDepRet_500 = 0;

                     clear_counter_flag = 'P';
                     clear_counter_flag_bna = 'P';
                     strMsgDate = "";
                     strMsgTime = "";
                     strRecordID = "";
                     strRecordID_BNA = "";
                    // objLogger.LogMsg("temp: I am here...");
                    try
                    {
                    for (int iRecFieldCount=0; iRecFieldCount<400; iRecFieldCount++)
                    {
                        if (strReplRecords[iCount, iRecFieldCount] == null || strReplRecords[iCount, iRecFieldCount].Contains("SUPERVISOR MODE EXIT"))
                        {
                            //skip looping if the repl data is completed
                            break;
                        } //
                        else if(strReplRecords[iCount,iRecFieldCount].Contains("DATE-TIME"))
                        {
                            strMsgDate = strReplRecords[iCount, iRecFieldCount].Substring(12, 8);
                            strMsgTime = strReplRecords[iCount, iRecFieldCount].Substring(21, 5);

                        }
                        //  Start getting DISP Details
                        else if(strReplRecords[iCount,iRecFieldCount].Contains("TYPE 1   TYPE 2")
                                && strReplRecords[iCount, iRecFieldCount+4].Contains("DISPENSED")
                            )
                        {
                            iRecFieldCount += 2;
                            strCass_Rej_A = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Rej_B = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Rem_A = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Rem_B = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Disp_A = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Disp_B = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Tot_A = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Tot_B = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount+=3;
                            strCass_Rej_C = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Rej_D = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Rem_C = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Rem_D = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Disp_C = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Disp_D = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                            iRecFieldCount++;
                            strCass_Tot_C = strReplRecords[iCount, iRecFieldCount].Substring(15, 5);
                            strCass_Tot_D = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);
                            
                            iRecFieldCount++;

                            /* UMER_MOD 20140409 starts*/
                            /* count the amounts based on denom values from config file */
                            try
                            {
                                if (strCass_Disp_A.Trim().Length > 0)
                                    strCass_Disp_A = (Convert.ToInt32(strCass_Disp_A.Trim()) * Convert.ToInt32(cass_type_1_denom)).ToString();
                                if (strCass_Disp_B.Trim().Length > 0)
                                    strCass_Disp_B = (Convert.ToInt32(strCass_Disp_B.Trim()) * Convert.ToInt32(cass_type_2_denom)).ToString();
                                if (strCass_Disp_C.Trim().Length > 0)
                                    strCass_Disp_C = (Convert.ToInt32(strCass_Disp_C.Trim()) * Convert.ToInt32(cass_type_3_denom)).ToString();
                                if (strCass_Disp_D.Trim().Length > 0)
                                    strCass_Disp_D = (Convert.ToInt32(strCass_Disp_D) * Convert.ToInt32(cass_type_4_denom)).ToString();

                                if (strCass_Rej_A.Trim().Length > 0)
                                    strCass_Rej_A = (Convert.ToInt32(strCass_Rej_A.Trim()) * Convert.ToInt32(cass_type_1_denom)).ToString();
                                if (strCass_Rej_B.Trim().Length > 0)
                                    strCass_Rej_B = (Convert.ToInt32(strCass_Rej_B.Trim()) * Convert.ToInt32(cass_type_2_denom)).ToString();
                                if (strCass_Rej_C.Trim().Length > 0)
                                    strCass_Rej_C = (Convert.ToInt32(strCass_Rej_C.Trim()) * Convert.ToInt32(cass_type_3_denom)).ToString();
                                if (strCass_Rej_D.Trim().Length > 0)
                                    strCass_Rej_D = (Convert.ToInt32(strCass_Rej_D.Trim()) * Convert.ToInt32(cass_type_4_denom)).ToString();

                                if (strCass_Rem_A.Trim().Length > 0)
                                    strCass_Rem_A = (Convert.ToInt32(strCass_Rem_A.Trim()) * Convert.ToInt32(cass_type_1_denom)).ToString();
                                if (strCass_Rem_B.Trim().Length > 0)
                                    strCass_Rem_B = (Convert.ToInt32(strCass_Rem_B.Trim()) * Convert.ToInt32(cass_type_2_denom)).ToString();
                                if (strCass_Rem_C.Trim().Length > 0)
                                    strCass_Rem_C = (Convert.ToInt32(strCass_Rem_C.Trim()) * Convert.ToInt32(cass_type_3_denom)).ToString();
                                if (strCass_Rem_D.Trim().Length > 0)
                                    strCass_Rem_D = (Convert.ToInt32(strCass_Rem_D.Trim()) * Convert.ToInt32(cass_type_4_denom)).ToString();

                                if (strCass_Tot_A.Trim().Length > 0)
                                    strCass_Tot_A = (Convert.ToInt32(strCass_Tot_A.Trim()) * Convert.ToInt32(cass_type_1_denom)).ToString();
                                if (strCass_Tot_B.Trim().Length > 0)
                                    strCass_Tot_B = (Convert.ToInt32(strCass_Tot_B.Trim()) * Convert.ToInt32(cass_type_2_denom)).ToString();
                                if (strCass_Tot_C.Trim().Length > 0)
                                    strCass_Tot_C = (Convert.ToInt32(strCass_Tot_C.Trim()) * Convert.ToInt32(cass_type_3_denom)).ToString();
                                if (strCass_Tot_D.Trim().Length > 0)
                                    strCass_Tot_D = (Convert.ToInt32(strCass_Tot_D.Trim()) * Convert.ToInt32(cass_type_4_denom)).ToString();
                            }
                            catch (Exception ex)
                            {
                                objLogger.LogMsg("Err: There is an error while calculating totals (denomval * notes) per cass type: exception is : "  + ex.Message);
                                //strMissingBulk = "103";
                            }
                            /* UMER_MOD 20140409 ends*/
                            if (strReplRecords[iCount, iRecFieldCount].Contains("LAST CLEARED"))
                            {
                                clear_counter_flag = 'P';
                                strDispLastClearedDate = strReplRecords[iCount, iRecFieldCount].Substring(15, 8);
                                strDispLastClearedTime = strReplRecords[iCount, iRecFieldCount].Substring(24, 5);

                                objDB.InsertEJ_Replinishment_DISP_Print(strGlobalTermID, strMsgDate, strMsgTime, strDispLastClearedDate, strDispLastClearedTime, clear_counter_flag,
                                    strCass_Disp_A, strCass_Disp_B, strCass_Disp_C, strCass_Disp_D, strCass_Rem_A, strCass_Rem_B, strCass_Rem_C, strCass_Rem_D,
                                    strCass_Rej_A, strCass_Rej_B, strCass_Rej_C, strCass_Rej_D,strCass_Tot_A, strCass_Tot_B, strCass_Tot_C, strCass_Tot_D,
                                     cass_type_1_curr , cass_type_2_curr,cass_type_3_curr, cass_type_4_curr,strTotalDispCass1,strTotalDispCass2,strTotalDispCass3,strTotalDispCass4,
                                     strTotalAmountSAR, strTotalAmountUSD, "NDCDISP", fileName, file_number, out strRecordID, strGlobalTermID, strGlobalFileDate);

                                strTotalAmountSAR = "0";
                                strTotalAmountUSD = "0";
                                strTotalDispCass1 = "0";
                                strTotalDispCass2 = "0";
                                strTotalDispCass3 = "0";
                                strTotalDispCass4 = "0";

                            }

                        }
                        // Now start getting BNA Details
                        // MOH - 22-02-2015 -Starts- Last Cleared information from the BNA BLOCK
                        /*else if (strReplRecords[iCount, iRecFieldCount].Contains("LAST CLEARED :"))
                        {
                            strBNALastClearedDate = strReplRecords[iCount, iRecFieldCount].Substring(17, 8);
                            strBNALastClearedTime = strReplRecords[iCount, iRecFieldCount].Substring(26, 5);
                            clear_counter_flag_bna = 'p';
                        }*/
                        // MOH - 22-02-2015 -Ends- Last Cleared information from the BNA BLOCK
                        else if (strReplRecords[iCount, iRecFieldCount].Contains("BNA AMOUNTS PER CASSETTE"))
                        {
                           iCashDep_50 = 0;
                           iCashDep_100 = 0;
                           iCashDep_200 = 0;
                           iCashDep_500 = 0;
                           
                           iDepRet_50 = 0;
                           iDepRet_100 = 0;
                           iDepRet_200 = 0;
                           iDepRet_500 = 0;
                                                     
                           try
                           {
                               // MOH -  14-02-2015 -  Starts - Adjusting the Line Counter to fetch the correct BNA Details //
                               // iRecFieldCount += 2;
                               iRecFieldCount ++;
                               // MOH - 14-02-2015 - Ends
                               if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM          CASS1     CASS2"))
                               {
                                   iRecFieldCount++;
                                   //these are cass1 and cass2 amounts
                                   if (strReplRecords[iCount, iRecFieldCount + 1].Contains("NO AMOUNTS FOR THESE CASSETTES"))
                                   {
                                       //do nothing and move to next record
                                       iRecFieldCount += 2;
                                   }
                                   else
                                   {
                                       while (strReplRecords[iCount, iRecFieldCount].Contains("SAR") && iRecFieldCount < 400)
                                       {
                                           //count all SAR Amounts
                                           switch (strReplRecords[iCount, iRecFieldCount].Substring(3, 3))
                                           {
                                               case "50":
                                                   iCashDep_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                   iCashDep_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                   iRecFieldCount++;
                                                   break;

                                               case "100":
                                                   iCashDep_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                   iCashDep_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                   iRecFieldCount++;
                                                   break;

                                               case "200":
                                                   iCashDep_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                   iCashDep_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                   iRecFieldCount++;
                                                   break;

                                               case "500":
                                                   iCashDep_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                   iCashDep_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                   iRecFieldCount++;
                                                   break;
                                               default: 
                                                   iRecFieldCount++;//UMER_MOD 140517
                                                   break;
                                           }//switch
                                       }//while
                                       // MOH -  14-02-2015 -  Starts - Adjusting the Line Counter to fetch the correct BNA Details //
                                       // iRecFieldCount += 4;
                                       iRecFieldCount++;
                                       // MOH - 14-02-2015 - Ends
                                       
                                   }//else

                               }////if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM          CASS1     CASS2"))
                            

                            if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM          CASS3     CASS4"))
                            {
                                iRecFieldCount++;
                                //these are cass3 and cass4 amounts
                                if (strReplRecords[iCount, iRecFieldCount+1].Contains("NO AMOUNTS FOR THESE CASSETTES"))
                                {
                                    //do nothing and move to next record
                                    iRecFieldCount +=2;
                                }
                                else
                                {
                                    while (strReplRecords[iCount, iRecFieldCount].Contains("SAR") && iRecFieldCount < 400)
                                    {
                                        //count all SAR Amounts
                                        switch (strReplRecords[iCount, iRecFieldCount].Substring(3, 3).Trim())
                                        {
                                            case "50":
                                                iCashDep_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                iCashDep_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "100":
                                                iCashDep_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                iCashDep_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "200":
                                                iCashDep_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                iCashDep_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "500":
                                                iCashDep_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                iCashDep_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;
                                            default:
                                                break;
                                        }//switch
                                    }//while
                                } //else
                                // MOH -  14-02-2015 -  Starts - Adjusting the Line Counter to fetch the correct BNA Details //
                                // iRecFieldCount += 4;
                                iRecFieldCount++;
                                // MOH - 14-02-2015 - Ends
                                
                            }//if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM          CASS3     CASS4"))

                            if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM         REJECT   RETRACT"))
                            {
                                iRecFieldCount++;
                                //these are reject and retract cassettes amounts
                                if (strReplRecords[iCount, iRecFieldCount + 1].Contains("NO AMOUNTS FOR THESE CASSETTES"))
                                {
                                    //do nothing and move to next record
                                    iRecFieldCount += 2;
                                }
                                else
                                {
                                    while (strReplRecords[iCount, iRecFieldCount].Contains("SAR") && iRecFieldCount < 400)
                                    {
                                        //count all SAR Amounts
                                        switch (strReplRecords[iCount, iRecFieldCount].Substring(3, 3).Trim())
                                        {
                                            case "50":
                                                // MOH - 16-02-2015 - Starts - Fetching the BNA Reject Values
                                                iDepRej_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                //MOH - 16-02-2015 - Ends
                                                iDepRet_50 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "100":
                                                // MOH - 16-02-2015 - Starts - Fetching the BNA Reject Values
                                                iDepRej_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                //MOH - 16-02-2015 - Ends
                                                iDepRet_100 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "200":
                                                // MOH - 16-02-2015 - Starts - Fetching the BNA Reject Values
                                                iDepRej_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                //MOH - 16-02-2015 - Ends
                                                iDepRet_200 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;

                                            case "500":
                                                // MOH - 16-02-2015 - Starts - Fetching the BNA Reject Values
                                                iDepRej_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(11, 9));
                                                //MOH - 16-02-2015 - Ends
                                                iDepRet_500 += Convert.ToInt32(strReplRecords[iCount, iRecFieldCount].Substring(21, 9));
                                                iRecFieldCount++;
                                                break;
                                            default:
                                                break;
                                        }//switch
                                    }//while
                                } //else
                                strRecordID_BNA = "";
                                //MOH - 16-02-2015 - Starts - Deposit Total Amount
                                do
                                {
                                    iRecFieldCount++;
                                }while (!strReplRecords[iCount, iRecFieldCount].Contains("TOTAL AMOUNTS PER CURRENCY"));
                                if (strReplRecords[iCount, iRecFieldCount].Contains("TOTAL AMOUNTS PER CURRENCY"))
                                {
                                    iRecFieldCount += 2;
                                    if (strReplRecords[iCount, iRecFieldCount].Contains("SAR") && iRecFieldCount < 400)
                                        strTotalAmountSAR = strReplRecords[iCount, iRecFieldCount].Substring(16, 9);
                                }
                                //MOH - 16-02-2015 - Ends - Deposit Total Amount
                                //MOH - 22-02-2015 - Starts - fetching Last Cleared Date and Time
                                do
                                {
                                    iRecFieldCount++;
                                }while(!strReplRecords[iCount,iRecFieldCount].Contains("LAST CLEARED :"));
                                
                                if (strReplRecords[iCount, iRecFieldCount].Contains("LAST CLEARED :"))
                                {
                                    strBNALastClearedDate = strReplRecords[iCount, iRecFieldCount].Substring(17, 8);
                                    strBNALastClearedTime = strReplRecords[iCount, iRecFieldCount].Substring(26, 5);
                                    clear_counter_flag_bna = 'C';
                                }
                                //MOH - 22-02-2015 - Ends - fetching Last Cleared Date and Time

                                objDB.InsertEJ_Replinishment_BNA_Print(strGlobalTermID, strMsgDate, strMsgTime, strBNALastClearedDate, strBNALastClearedTime, clear_counter_flag_bna,
                                    iCashDep_50.ToString(), iCashDep_100.ToString(), iCashDep_200.ToString(), iCashDep_500.ToString(),
                                    iDepRet_50.ToString(), iDepRet_100.ToString(), iDepRet_200.ToString(), iDepRet_500.ToString(), iDepRej_50.ToString(), iDepRej_100.ToString(), 
                                    iDepRej_200.ToString(), iDepRej_500.ToString(), strTotalAmountSAR.ToString(), "NDCBNA", fileName, 
                                    file_number, out strRecordID_BNA, strGlobalTermID, strGlobalFileDate);
                                strTotalAmountSAR = "0";
                            }//if (strReplRecords[iCount, iRecFieldCount].Contains("DENOM         REJECT   RETRACT"))
                        
                         }//try
                            
                                catch(Exception ex)
                                {
                                    objLogger.LogMsg("Error in Repl Processing with exception in BNA AMOUNTS PER CASSETTE : Exeption is " + ex.Message);   
                                }
                          
                        }//else if (strReplRecords[iCount, iRecFieldCount].Contains("BNA AMOUNTS PER CASSETTE"))
                        //  Coin Area Starts // IRFAN_MOD
                        else if (strReplRecords[iCount, iRecFieldCount].Contains("TOTAL COUNTS PER BIN"))
                        {
                            string[] COIN_DEP_BIN = new string[CoinBINS.Length]; ;
                           string COIN_DEP_RET="" ;
                           string TOT_COIN_DEP_AMT ="";
                           string COIN_DEP_AMT = "";
                           string Coin_LastClearedDate = "", Coin_LastClearedTime="";
                           string Coin_clear_counter_flag = "";

                           string aLine = "";

                           do
                           {
                               iRecFieldCount++;
                               aLine = strReplRecords[iCount, iRecFieldCount].Replace(" ","" );

                           } while (!aLine.Contains("RETRACTBIN") && !aLine.Contains("LASTCLEARED") && iRecFieldCount < 400);

                           if (aLine.Contains("RETRACTBIN"))
                           {
                               COIN_DEP_RET = aLine.Substring(aLine.IndexOf("RETRACTBIN") + "RETRACTBIN".Length).Trim();
                               iRecFieldCount++;
                               aLine = strReplRecords[iCount, iRecFieldCount].Replace(" ", ""); ;
                           }
                           if (aLine.Contains("LASTCLEARED:"))
                           {
                               Coin_LastClearedDate = aLine.Substring(aLine.IndexOf("LASTCLEARED:") + "LASTCLEARED:".Length, 8);
                               Coin_LastClearedTime = aLine.Substring(aLine.IndexOf("LASTCLEARED:") + "LASTCLEARED:".Length + 8 );
                               Coin_clear_counter_flag = "C";
                               iRecFieldCount++;
                           }

                           while (!aLine.Contains("TOTAL=") && iRecFieldCount < 400)
                           {
                               aLine = strReplRecords[iCount, iRecFieldCount].Replace(" ", ""); ;

                               for (int c = 0; c < CoinBINS.Length; c++)
                               {
                                   if (aLine.Contains(CoinBINS[c]))
                                          COIN_DEP_BIN[c] = Regex.Match(aLine.Substring(aLine.IndexOf("=") + 1), @"\d*\.?\d+").Value; ;


                               }
                               iRecFieldCount++;
                          }
                        // Total from file
                        if (aLine.Contains("TOTAL="))
                            TOT_COIN_DEP_AMT = Regex.Match(aLine.Substring(aLine.IndexOf("TOTAL=") + 6), @"\d*\.?\d+").Value;

                           // Compute total by summing bin
                           COIN_DEP_AMT = "0.00";

                           try
                           {
                               Decimal compulteTotal = 0;
                               for (int c = 0; c < CoinBINS.Length; c++)
                                   compulteTotal = compulteTotal + Convert.ToDecimal(COIN_DEP_BIN[c]);

                               COIN_DEP_AMT = compulteTotal.ToString();

                           }
                           catch (Exception ex)
                           {
                               objLogger.LogMsg("Error Compute Coin Deposit Total :   NCR Source: " + ex.Source + " , Message: " + ex.Message
                                                    + ", File Name and Number : " + fileName );
                                COIN_DEP_AMT = "-1";
                           }

                           objDB.InsertEJ_Replinishment_Coin(strGlobalTermID, strMsgDate, strMsgTime, Coin_LastClearedDate, Coin_LastClearedTime, Coin_clear_counter_flag,
                             COIN_DEP_BIN,COIN_DEP_RET, TOT_COIN_DEP_AMT, COIN_DEP_AMT, "NDCCOIN", fileName,
                               file_number, out strRecordID_BNA, strGlobalTermID, strGlobalFileDate);
                        
                         

                        } //  Coin Area Ends
                      else if (strReplRecords[iCount, iRecFieldCount].Contains("CASH COUNTS CLEARED"))
                        {
                            clear_counter_flag = 'C';

                            strMsgDate = strReplRecords[iCount, iRecFieldCount - 1].Substring(5, 6) + strReplRecords[iCount, iRecFieldCount - 1].Substring(13, 2);
                            strMsgTime = strReplRecords[iCount, iRecFieldCount - 1].Substring(16, 5);
                            //clear_counter_flag_bna = 'C';


                            if (strRecordID != "")
                                objDB.UpdateEJ_Replinishment_DISP_Clear(strRecordID,strMsgDate,strMsgTime);
                            /*
                            if (strRecordID_BNA!="")
                                objDB.UpdateEJ_Replinishment_BNA_Clear(strRecordID_BNA);*/
                        }
                        else if (strReplRecords[iCount, iRecFieldCount].Contains("INIT BNA STARTED - RETRACT BIN"))
                        {
                            clear_counter_flag_bna = 'C';

                            //strMsgDate = strReplRecords[iCount, iRecFieldCount - 1].Substring(5, 6) + strReplRecords[iCount, iRecFieldCount - 1].Substring(13, 2);
                           /*UMER_MOD 140430 starts */
                            /* Inma Files are not containing date/time thus not updating the time here*/
                            //strMsgTime = strReplRecords[iCount, iRecFieldCount ].Substring(0, 5);
                            //clear_counter_flag_bna = 'C';

                            
                            if (strRecordID_BNA != "")
                                objDB.UpdateEJ_Replinishment_BNA_Clear(strRecordID_BNA, strMsgDate, strMsgTime);
                            /*UMER_MOD 140430 ends */
                            /*
                            if (strRecordID_BNA!="")
                                objDB.UpdateEJ_Replinishment_BNA_Clear(strRecordID_BNA);*/
                        }
                      


                        else if (strReplRecords[iCount, iRecFieldCount].Contains("CASH ADDED"))
                        {
                            clear_counter_flag = 'A';
                            strMsgDate = strReplRecords[iCount, iRecFieldCount - 1].Substring(5, 6) + strReplRecords[iCount, iRecFieldCount - 1].Substring(13, 2);
                            strMsgTime = strReplRecords[iCount, iRecFieldCount - 1].Substring(16, 5);
                          
                            iRecFieldCount++;
                            
                            if (strReplRecords[iCount, iRecFieldCount].Contains("TYPE 1"))
                            {
                                strCass_Rem_A = strReplRecords[iCount, iRecFieldCount].Substring(9, 5).Trim();
                                strCass_Rem_B = strReplRecords[iCount, iRecFieldCount].Substring(25, 5).Trim();
                                iRecFieldCount++;
                                strCass_Rem_C = strReplRecords[iCount, iRecFieldCount].Substring(9, 5).Trim();
                                strCass_Rem_D = strReplRecords[iCount, iRecFieldCount].Substring(25, 5).Trim();
                                /*UMER_MOD 140430 starts*/
                                //added to calculated actual amounts for the cash added 

                                if (strCass_Rem_A.Trim().Length > 0)
                                    strCass_Rem_A = (Convert.ToInt32(strCass_Rem_A.Trim()) * Convert.ToInt32(cass_type_1_denom)).ToString();
                                if (strCass_Rem_B.Trim().Length > 0)
                                    strCass_Rem_B = (Convert.ToInt32(strCass_Rem_B.Trim()) * Convert.ToInt32(cass_type_2_denom)).ToString();
                                if (strCass_Rem_C.Trim().Length > 0)
                                    strCass_Rem_C = (Convert.ToInt32(strCass_Rem_C.Trim()) * Convert.ToInt32(cass_type_3_denom)).ToString();
                                if (strCass_Rem_D.Trim().Length > 0)
                                    strCass_Rem_D = (Convert.ToInt32(strCass_Rem_D.Trim()) * Convert.ToInt32(cass_type_4_denom)).ToString();

                                /*UMER_MOD 140430 ends*/

                                objDB.InsertEJ_Replinishment_DISP_Add(strGlobalTermID, strMsgDate, strMsgTime, clear_counter_flag,
                                    strCass_Rem_A, strCass_Rem_B, strCass_Rem_C, strCass_Rem_D, "NDCDISP", fileName,file_number
                                    , strGlobalTermID, strGlobalFileDate);

                            }
                        }
                    }//for (int iRecFieldCount=0; iRecFieldCount<400; iRecFieldCount++)
                }//try
                catch (Exception ReplEx)
                    {
                        objLogger.LogMsg("There was exception in Replenishment Loading:  " + ReplEx.Message);
                    }
                }//for (int iCount = 0; iCount <replCounter; iCount++)

                //objLogger.LogMsg("temp3: There No of txn are:  " + transactionCounter);
                strTermID = strGlobalTermID;
                strNoOfTxn = transactionCounter.ToString();
                strNoOfStatus = statusCounter.ToString();
                //objLogger.LogMsg("the no of tranx and statuses are  " + transactionCounter + strNoOfTxn + " " + strNoOfStatus);
                if (fileName.Length > 8)
                {
                    strFdate = strGlobalFileDate;  //fileName.Substring(17, 8);
                    
                }
                return 0;
            }
            else
            {
                objLogger.LogMsg("Error: File does not exist: "+ filePath+fileName);
                return -1;
            }
        }
        public string RemoveControlCharsFromString(string inputStr)
        {
            StringBuilder replacedString = new StringBuilder();


            foreach (char c in inputStr)
            {

                if (c != 0x58 && c != 0x2e && c != 0x0d && c != 0x0f && c != 0x0a /*&& c != 0x20*/ && c != 0x8e && c != 0x1b && c != 0x0e)
                {

                    replacedString.Append(c);
                }
                else
                {
                    if (c == 0x58)
                        replacedString.Append("X");
                    else if (c == 0x2e)
                        replacedString.Append(".");
                    else if (c == 0x0d || c == 0x0f || c == 0x0a /*|| c == 0x20*/)
                        replacedString.Append("");
                    //else if (CheckChar == "20")
                    //    sb.Append("v");
                    else if (c == 0x8e)
                        replacedString.Append("v");
                    else if (c == 0x1b)
                        replacedString.Append("v");
                    else if (c == 0x0e)
                        replacedString.Append(" ");

                }

            }

            replacedString = replacedString.Replace("*v(Iwv(1*", "*w*");
            replacedString = replacedString.Replace("v(I", "");
            replacedString = replacedString.Replace("v(1", "");
            replacedString = replacedString.Replace("v[020t", "");
            replacedString = replacedString.Replace("v[020", "");
            replacedString = replacedString.Replace("v[0r", "");
            replacedString = replacedString.Replace("v[000p", "");
            replacedString = replacedString.Replace("v[040qve1vw3vh162", "");
            replacedString = replacedString.Replace("v[040q", "");
            replacedString = replacedString.Replace("v)2", "");
            replacedString = replacedString.Replace("v[05p", "");
            replacedString = replacedString.Replace("v[00p", "");
            return replacedString.ToString();

            /* removing control characters and preparing an ascii output ends*/

        }

    }
}
