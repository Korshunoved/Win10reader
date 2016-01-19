using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.LibraryTools
{
    public interface IExpiredCallBack
    {
        void ExpiredCallBack(Book book);
    }
}
