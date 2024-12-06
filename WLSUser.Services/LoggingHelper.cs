using System;
using Microsoft.AspNetCore.Http;

namespace WLSUser.Services
{
    public class LoggingHelper
    {
        public static Guid GetTransactionID(IHeaderDictionary headers)
        {
            //If unable to find a valid WLSTransactionID in the headers, create a new transaction ID for this request

            if (headers == null || string.IsNullOrEmpty(headers["WLSTransactionID"]))
                return Guid.NewGuid();
            else
            {
                if (Guid.TryParse(headers["WLSTransactionID"], out Guid retval))
                {
                    return retval;
                }
                else
                    return Guid.NewGuid();
            }
        }
    }
}