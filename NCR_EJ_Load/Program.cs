using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace NCR_EJ_Load
{
    class Program
    {

        static string ConnectionData,global_bank_bin;
        public static string ConString
        {
            get { return ConnectionData; }
            set { ConnectionData = value; }
        }

        static void Main(string[] args)
        {
            Logger objLogger = new Logger();
            
            if (args.Length < 1)
            {

                Console.WriteLine("You must provide the inputfiles list as parameter..");
                objLogger.LogMsg("Abnormal Close: inputfiles list was missing as parameter.");
                return;
            }
            
          
            //Getting the Configration of the EJ Upload program ..
            ConfigReader objCfg = new ConfigReader();
            ArrayList objArr_Config = new ArrayList();
            objArr_Config = objCfg.ReadConfig();
            if (objArr_Config == null)
                return;

            Console.Title = "ReconSS - NCR EJ Load";

            Console.WriteLine("NCR EJ Loading Started at : " + DateTime.Now.ToString());

            ConString = " Data Source =" + objArr_Config[0].ToString() + "; User Id = " + objArr_Config[1].ToString() + " ; Password =" + objArr_Config[2].ToString() + ";";
            global_bank_bin = objArr_Config[24].ToString();
            EJProcessor objEJProcessor = new EJProcessor(ConString); 
            DBProcessor objDBProcessor = new DBProcessor(ConString,global_bank_bin);
            

            objEJProcessor.strThreshold = objArr_Config[22].ToString();
            objEJProcessor.cass_type_1_denom = objArr_Config[5].ToString();
            objEJProcessor.cass_type_2_denom = objArr_Config[8].ToString();
            objEJProcessor.cass_type_3_denom = objArr_Config[11].ToString();
            objEJProcessor.cass_type_4_denom = objArr_Config[14].ToString();

            objEJProcessor.cass_type_1_curr = objArr_Config[6].ToString();
            objEJProcessor.cass_type_2_curr = objArr_Config[9].ToString();
            objEJProcessor.cass_type_3_curr = objArr_Config[12].ToString();
            objEJProcessor.cass_type_4_curr = objArr_Config[15].ToString();
            objEJProcessor.convert_file = objArr_Config[23].ToString();
            objEJProcessor.global_bank_bin = global_bank_bin;
            
            // COIN Starts
            EJProcessor.patternCoin = objArr_Config[27].ToString();
            EJProcessor.patternNotes = objArr_Config[28].ToString();
            EJProcessor.CoinBINS = objArr_Config[29].ToString().Split(',');

            //Regex rgxCoin = new Regex(objEJProcessor.patternCoin);
            //Regex rgxNotes = new Regex(objEJProcessor.patternNotes);
            //string test = "1.00X-2,0.50X-2,0.25X456456456";
            //if (rgxCoin.IsMatch(test) )//|| rgxNotes.IsMatch(test))
            //{
            //    test=test;
            //}
            // Coin Ends

            int iTotalProcessedRecords = 0;
            int iTotalRejectedRecords = 0;
            int iTotalRecords = 0;
            string strRecordDate = "";
            string strFilesDate="";
            string strFilesDateMin = "";
            int iEPD_CODE = 0;
            string sFileNumber = "";

            if (File.Exists(args[0]) == false)
            {
                string strErrorMessage;
                strErrorMessage = "The file containing inputfiles list does not exist.." + args[0];
                Console.WriteLine(strErrorMessage);
                objLogger.LogMsg("Abnormal Close: " + strErrorMessage);
                if (objDBProcessor.EndApplicationJobWithError(strErrorMessage))
                {
                    return;
                }
                else
                {
                    Console.WriteLine("Abnormal Close: EndApplicationJobWithError returned false");
                    objLogger.LogMsg("Abnormal Close: EndApplicationJobWithError returned false");
                    return;
                }
            }

            
            sFileNumber = objDBProcessor.GetNextFileID();

            string[] strFilesToProcess = File.ReadAllLines(args[0]);

            iTotalRecords = strFilesToProcess.Length;

            //UMER_MOD 20160228 starts 
            string strThresholdMinFiles = objArr_Config[25].ToString();
            int iThresholdMinFiles = int.Parse(strThresholdMinFiles.Trim());

            string strThresholdGrace = objArr_Config[26].ToString();
            int iThresholdGrace = int.Parse(strThresholdGrace.Trim());


            if (iTotalRecords < iThresholdMinFiles)
            {
                string strErrorMessage;
                strErrorMessage = "The EJ Process can not complete; There are only " + iTotalRecords +
                    " Input Files while Minimum Threshold Value is configured as: " + iThresholdMinFiles;

                Console.WriteLine(strErrorMessage);
                objLogger.LogMsg("Abnormal Close: " + strErrorMessage);
                if (objDBProcessor.EndApplicationJobWithError(strErrorMessage))
                {
                    return;
                }
                else
                {
                    Console.WriteLine("Abnormal Close: EndApplicationJobWithError returned false");
                    objLogger.LogMsg("Abnormal Close: EndApplicationJobWithError returned false");
                    return;
                }
            }
            objLogger.LogMsg("No of Files are more than threshold value: " + iTotalRecords + ">" + iThresholdMinFiles);
            //UMER_MOD 20160228 ends
            //UMER_MOD 20170515 starts
            //do not take the record date as sys date, but take the maximum date of the input files list


            strRecordDate = (DateTime.Now.Year.ToString().Substring(2, 2) + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0'));//UMER_MOD 20170524 

            strFilesDate = strFilesToProcess.Select(v => int.Parse(v.Substring(10, 6))).Max().ToString();//UMER_MOD 20170524 
            
            //UMER_MOD 20170530 starts
            strFilesDateMin = strFilesToProcess.Select(v => int.Parse(v.Substring(10, 6))).Min().ToString();
            if (strFilesDate != strFilesDateMin)
            {
                string strErrorMessage;
                string strMonitorMessage;
                strErrorMessage = "Abnormal Close: EJ Proccess Input Files are having multiple dates";
                strMonitorMessage = "Abnormal Close: EJ Proccess Input Files are having multiple dates";
                Console.WriteLine(strErrorMessage);
                objLogger.LogMsg( strErrorMessage);
                if (objDBProcessor.EndApplicationJobWithError(strMonitorMessage))
                {
                    return;
                }
                else
                {
                    Console.WriteLine("Abnormal Close: EndApplicationJobWithError returned false");
                    objLogger.LogMsg("Abnormal Close: EndApplicationJobWithError returned false");
                    return;
                }

            }
            //UMER_MOD 20170530 ends


            //UMER_MOD 20170515 ends

            
            //UMER_MOD 20160228 starts
            int iRemainingRcds = 0;
          //  objDBProcessor.Chk_EJ_Process_Log_for_PrevDay(strRecordDate, ref iRemainingRcds);
            objDBProcessor.Chk_EJ_Process_Log_for_PrevDay(strFilesDate, ref iRemainingRcds);//UMER_MOD 20170524
            if (iRemainingRcds>0)
            {
                /*UMER_MOD 201704 23 starts */
                /* Configuration check if the previous day files in folder are reaching minimum threshold */
                //UMER_MOD 20170515 starts
                //DateTime yesterdayDt = DateTime.Now.Date.AddDays(-1);
                //string stryesterdayDt = (yesterdayDt.Year.ToString() + yesterdayDt.Month.ToString().PadLeft(2, '0') + yesterdayDt.Day.ToString().PadLeft(2, '0'));
                //DateTime yesterdayDt = DateTime.ParseExact(strRecordDate, "yyMMdd", System.Globalization.CultureInfo.InvariantCulture);//UMER_MOD 20170524
                DateTime yesterdayDt = DateTime.ParseExact(strFilesDate, "yyMMdd", System.Globalization.CultureInfo.InvariantCulture);//UMER_MOD 20170524
                yesterdayDt = yesterdayDt.AddDays(-1);
                string stryesterdayDt = (yesterdayDt.Year.ToString() + yesterdayDt.Month.ToString().PadLeft(2, '0') + yesterdayDt.Day.ToString().PadLeft(2, '0'));
                //UMER_MOD 20170515 ends
                                //int iTotalYesterdayRcds = 0;
                //int iTotalYesterdayRcds = iThresholdGrace;//UMER_MOD 20170516 //add a grace to allow loading even if 123 is missing
                int iTotalYesterdayRcds = 0; //UMER_MOD 20170524 

                for (int iLoop = 0; iLoop < iTotalRecords; iLoop++)
                {
                    if (strFilesToProcess[iLoop].Contains("_" + stryesterdayDt))
                        iTotalYesterdayRcds++;
                }
                /*UMER_MOD 201704 23 ends */

                // if (iTotalYesterdayRcds < iThresholdMinFiles && iTotalYesterdayRcds<iRemainingRcds)//UMER_MOD 20170524 
                if (iTotalYesterdayRcds < iThresholdMinFiles && (iTotalYesterdayRcds + iThresholdGrace) < iRemainingRcds)//UMER_MOD 20170524 
                {
                    string strErrorMessage;
                    string strMonitorMessage;
                    strErrorMessage = "The EJ Process is not executed for the prvious day while running on:" + strRecordDate + 
                        " and only contains " + iTotalYesterdayRcds + " Input Files while Minimum Threshold Value is configured as: " 
                        + iThresholdMinFiles +" And Remaing are: "+ iRemainingRcds;
                    
                    strMonitorMessage = "Failed: Not executed for Prev Day, Yesterday Records / Grace Value/ Threshold/ Remaining: " +
                        iTotalYesterdayRcds + " / " + iThresholdGrace + " / " + iThresholdMinFiles + " / " + iRemainingRcds;
                    
                    Console.WriteLine(strErrorMessage);
                    objLogger.LogMsg("Abnormal Close: " + strErrorMessage);
                    if (objDBProcessor.EndApplicationJobWithError(strMonitorMessage))
                    {
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Abnormal Close: EndApplicationJobWithError returned false");
                        objLogger.LogMsg("Abnormal Close: EndApplicationJobWithError returned false");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("The EJ Process is not executed for the prvious day while running on:" + strFilesDate + ", but contains " + iTotalYesterdayRcds + " Input Files with Threshold/Grace Value as: " + iThresholdMinFiles+"/"+iThresholdGrace + " And Remaing are: "+ iRemainingRcds+ " ,Therefore it will continue Processing");
                    objLogger.LogMsg("The EJ Process is not executed for the prvious day while running on:" + strFilesDate + ", but contains " + iTotalYesterdayRcds + " Input Files with Threshold/Grace Value as: " + iThresholdMinFiles + "/" + iThresholdGrace + " And Remaing are: " + iRemainingRcds + " ,Therefore it will continue Processing");

                }
            }

            //UMER_MOD 20160228 ends


            if (objDBProcessor.Insert_EJ_Process_Log(strRecordDate, sFileNumber,
                 //UMER_MOD 140519 starts
                 //take file_Date from filename xxxx_20140519_xxxx
                 //strFilesToProcess[0].Substring(strFilesToProcess[0].Length - 6, 6),
                 strFilesToProcess[0].Substring(strFilesToProcess[0].IndexOf("_") + 3, 6),
                //UMER_MOD 140519 ends
                 DateTime.Now.Hour.ToString().PadLeft(2, '0') + ":" + DateTime.Now.Minute.ToString().PadLeft(2, '0'), "", "001", 0, 0, iTotalRecords, out iEPD_CODE))
            {
                try
                {
                    objEJProcessor.file_number = Convert.ToInt32(sFileNumber);
                }
                catch (Exception ex)
                {
                    objLogger.LogMsg("Error in file number conversion: " + sFileNumber +" Exception is:"+ex.Message);
                    objEJProcessor.file_number = 1;
                }
                    //insert into  application job log the job status to be started
                //UMER_MOD 170614 starts
                //bank requirement, yousuf's email 170614, application job should have file date inspite or sysdate
                //objDBProcessor.StartApplicationJob(strRecordDate);
                objDBProcessor.StartApplicationJob(strFilesDate);
                //UMER_MOD 170614 ends

                for (int i = 0; i < strFilesToProcess.Length; i++)
                {
                    if (strFilesToProcess[i].Trim().Length > 0)
                    {
                        Console.WriteLine("Processing File: " + objArr_Config[3].ToString() + strFilesToProcess[i]);
                        if (File.Exists(objArr_Config[3].ToString() + strFilesToProcess[i]))
                        {
                            Console.WriteLine(strFilesToProcess[i]);
                            Console.WriteLine("Processing File: " + strFilesToProcess[i]);
                            objLogger.LogMsg("Processing File: " + strFilesToProcess[i]);

                            if (objEJProcessor.ProcessEJFile(objArr_Config[3].ToString(), strFilesToProcess[i]) == 0)
                            {
                                //UMER_MOD 20141208 starts
                                /*objDBProcessor.UpdateFileProcessStatus(objEJProcessor.strTermID, objEJProcessor.strNoOfTxn,
                                    objEJProcessor.strNoOfStatus, objEJProcessor.strFdate, objEJProcessor.strMissingBulk, strFilesToProcess[i]);
                                */
                                objDBProcessor.UpdateFileProcessStatus(objEJProcessor.strTermID, objEJProcessor.strNoOfTxn,
                                    objEJProcessor.strNoOfStatus, objEJProcessor.strFdate, objEJProcessor.strMissingBulk, strFilesToProcess[i],sFileNumber);

                                //UMER_MOD 20141208 ends
                                Console.WriteLine("Successfully Processed : " + strFilesToProcess[i]);
                                objLogger.LogMsg("Successfully Processed : " + strFilesToProcess[i]);
                                iTotalProcessedRecords++;
                                /* try
                                 {
                                     strTotalProcessedRecords = (Convert.ToInt32(strTotalProcessedRecords) + Convert.ToInt32(objEJProcessor.strNoOfTxn) +
                                         Convert.ToInt32(objEJProcessor.strNoOfTxn)).ToString();

                                 }
                                 catch (Exception ex)
                                 {
                                     objLogger.LogMsg("Exception in calculating the total no of records from file : " + strFilesToProcess[i] + " , Exception says: " +ex.Message);
                                 }*/
                            }
                            else
                            {
                                /* if(strFilesToProcess[i].Length>8)
                                     objDBProcessor.UpdateFileProcessStatus(" ","0","0",strFilesToProcess[i].Substring(strFilesToProcess[i].Length -8,8),"104",strFilesToProcess[i]);
                                 else
                                     objDBProcessor.UpdateFileProcessStatus(" ", "0", "0", "", "104", strFilesToProcess[i]);*/
                                iTotalRejectedRecords++;
                                Console.WriteLine("Error: File processing : " + strFilesToProcess[i]);
                                objLogger.LogMsg("Error: File processing : " + strFilesToProcess[i]);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Input file does not exist: " + strFilesToProcess[i] + " continuing for the rest of the list..");
                            objLogger.LogMsg("Input file does not exist: " + strFilesToProcess[i] + " continuing for the rest of the list..");
                        }

                    }
                }
               
                //update application job process status to be completed

                if (iTotalRecords == iTotalRejectedRecords)
                {
                    string strErrorMessage;
                    string strMonitorMessage;
                    strErrorMessage = "Successful- while all input files were Duplicate";
                    strMonitorMessage = "Successful- while all input files were Duplicate";
                    Console.WriteLine(strErrorMessage);
                    objLogger.LogMsg(strErrorMessage);
                    objDBProcessor.EndApplicationJobWithError(strMonitorMessage);
                    
                }
                else
                    objDBProcessor.EndApplicationJob();

                if (objDBProcessor.Update_EJ_PROCESS_LOG(strRecordDate, iTotalProcessedRecords, iTotalRejectedRecords, iEPD_CODE))
                {
                    objLogger.LogMsg("NCR EJ Program Completed Successfully - and EJ_PROCESS_LOG updated successfully");
                    Console.WriteLine("NCR EJ Program Completed Successfully at: " + DateTime.Now.ToString());
                    //Console.ReadLine();
                }
                else
                {
                    objLogger.LogMsg("NCR EJ Program Completed But EJ_PROCESS_LOG could not be updated, which is critical please check logs.");
                    Console.WriteLine("NCR EJ Program Completed But EJ_PROCESS_LOG could not be updated, which is critical please check logs.");                    
                   // Console.ReadLine();
                }

            }//if ej_process_log insertion is ok
            else
            {
                Console.WriteLine("ERROR Abnormal Exit as the EJ_PROCESS_LOG could not be updated properly with the given date: " );
                objLogger.LogMsg("ERROR Abnormal Exit as the EJ_PROCESS_LOG could not be updated properly with the given date: ");
            }
            
        }
    }
}
