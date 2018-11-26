using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml;

namespace NCR_EJ_Load
{
    class ConfigReader
    {
        Logger objLogger;
        XmlDocument ConfigDoc;

        public ConfigReader()
        {
            objLogger = new Logger();
        }

        public ArrayList ReadConfig()
        {
            if (!File.Exists("edcload_ncr.cfg"))
            {
                System.Console.WriteLine("Configuratoin File edcload_ncr.cfg is not available, Quiting");
                return null;
            }

            ConfigDoc = new XmlDocument();
            try
            {
            objLogger.LogMsg ("Function : ReadConfig -- Loading Config File .. ");
            ConfigDoc.Load(@"edcload_ncr.cfg");
            objLogger.LogMsg("Function : ReadConfig -- Config File Loaded .. ");
            ArrayList arrResult = new ArrayList();
            ArrayList arrTemp = new ArrayList();
           
                arrTemp = GetConfigData("data_source");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("db_user");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("db_pass");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ejpath");
                arrResult.Add(arrTemp[0]);
                arrResult.Add("A");
                arrTemp = GetConfigData("ncr_cass_A");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_A");
                arrResult.Add(arrTemp[0]);

                arrResult.Add("B");
                arrTemp = GetConfigData("ncr_cass_B");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_B");
                arrResult.Add(arrTemp[0]);

                arrResult.Add("C");
                arrTemp = GetConfigData("ncr_cass_C");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_C");
                arrResult.Add(arrTemp[0]);

                arrResult.Add("D");
                arrTemp = GetConfigData("ncr_cass_D");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_D");
                arrResult.Add(arrTemp[0]);

                arrResult.Add("E");
                arrTemp = GetConfigData("ncr_cass_E");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_E");
                arrResult.Add(arrTemp[0]);

                arrResult.Add("F");
                arrTemp = GetConfigData("ncr_cass_F");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("ncr_curr_F");
                arrResult.Add(arrTemp[0]);
                
                arrTemp = GetConfigData("ncr_threshold");
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("convert_file");
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("bank_bin");
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("min_file_threshold");
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("threshold_grace");//UMER_MOD 20170516 //add a grace to allow loading even if 123 is missing
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("coin_pattern");// IRFAN_MOD Coins
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("cash_pattern");//IRFAN_MOD Cash
                arrResult.Add(arrTemp[0]);

                arrTemp = GetConfigData("coin_bins");// IRFAN_MOD Coin Bins 7 as of now
                arrResult.Add(arrTemp[0]);
            
                objLogger.LogMsg("Function : ReadConfig -- Config Params Loaded .. ");
                return arrResult;    
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Exception in Reading Configuration : " + ex.Message);
                return null;
            }
        }

        public ArrayList ReadParsingConfig()
        {
            ConfigDoc = new XmlDocument();
            //objLogger.LogMsg("Function : ReadParsingConfig -- Loading Config File .. ");
            ConfigDoc.Load(@"edcload_ncr_parsing.cfg");
            //objLogger.LogMsg("Function : ReadParsingConfig -- Config File Loaded .. ");
            ArrayList arrResult = new ArrayList();
            ArrayList arrTemp = new ArrayList();
            try
            {
                arrTemp = GetConfigData("StartSentinel");
                arrResult.Add(arrTemp[0]);
                arrTemp = GetConfigData("EndSentinel");
                arrResult.Add(arrTemp[0]);
                //objLogger.LogMsg("Function : ReadParsingConfig -- Config Params Loaded .. ");
                return arrResult;
            }
            catch (Exception ex)
            {
                objLogger.LogMsg("Exception in Reading Parsing Configuration : " + ex.Message);
                return null;
            }
        }
        private ArrayList GetConfigData(string ConfigTag)
        {
            ArrayList arrConfig = new ArrayList();
            try
            {
                XmlNodeList Nodelist_Config = ConfigDoc.GetElementsByTagName(ConfigTag);
                XmlNodeList ChilNodeList = Nodelist_Config[0].ChildNodes;
                for (int i = 0; i < ChilNodeList.Count; i++)
                {
                    arrConfig.Add(ChilNodeList[i].InnerText);
                }
                return arrConfig;
            }
            catch (Exception ex)
            {
                string errorMsg = "Exception : Function: GetConfigData, Parameter: ConfigTag = " + ConfigTag + ", Source: + " +
                    ex.Source + ", Message: " + ex.Message;
                objLogger.LogMsg(errorMsg);
                arrConfig.Clear();
                return arrConfig;
            }
        }



    }
}
