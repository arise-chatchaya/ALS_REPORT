using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Access_data.Model
{
    public class Report02_Model
    {
        public string Package_No { get; set; }
        public string BA_No { get; set; }
        public string Invoice_No { get; set; }
        public DateTime? Invoice_Issue_Date { get; set; }
        public string BA_ID { get; set; }
        public string Invoice_Note { get; set; }
        public string Invoice_Process { get; set; }
    }
}
