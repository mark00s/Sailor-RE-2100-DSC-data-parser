using BSc_Thesis.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSc_Thesis.Models
{

    interface IFileManager
    {
        void Open();
        void Delete();
        void OpenFolder();
        void SelectFolder();
    }
}
