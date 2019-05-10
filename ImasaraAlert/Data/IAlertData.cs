using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImasaraAlert.Data
{
    interface IAlertData
    {

        string Ng { set; get; }
        string Id { set; get; }
        string Name { set; get; }
        DateTime Last_Date { set; get; }
        DateTime Regist_Date { set; get; }
        bool Pop { set; get; }
        bool Web { set; get; }
        bool Sound { set; get; }
        bool App_a { set; get; }
        bool App_b { set; get; }
        bool App_c { set; get; }
        bool App_d { set; get; }
        string Memo { set; get; }

    }
}
