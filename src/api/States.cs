using System;

namespace CodeFlip.CodeJar.Api
{
    public static class States
    {
        public static byte Generated = 0;
        public static byte Active = 1;
        public static byte Expires = 2;
        public static byte Redeemed = 3;
        public static byte Inactive = 4;

        public static string ConvertToString(byte state)
        {
            var stringValue = "";

            switch(state)
            {
                case 0:
                stringValue = "Generated";
                break;

                case 1:
                stringValue = "Active";
                break;

                case 2:
                stringValue = "Expires";
                break;

                case 3:
                stringValue = "Redeemed";
                break;

                case 4: 
                stringValue = "Inactive";
                break;

                default:
                stringValue = "";
                break;
            }
            return stringValue;
        }

        public static byte ConvertToByte(string state)
        {
            byte byteValue = 0;

            switch(state)
            {
                case "Generated":
                byteValue = 0;
                break;

                case "Active":
                byteValue = 1;
                break;

                case "Expires":
                byteValue = 2;
                break;

                case "Redeemed":
                byteValue = 3;
                break;

                case "Inactive":
                byteValue = 4;
                break;
            }
            return byteValue;
        }
    
    }

    
}