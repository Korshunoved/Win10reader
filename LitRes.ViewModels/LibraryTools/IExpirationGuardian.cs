using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.LibraryTools
{
    public interface IExpirationGuardian
    {
        void StartGuardian();
        void AddBook(Book book);
        void AddCallBack(IExpiredCallBack callBack);
        void RemoveCallBack(IExpiredCallBack callBack);
    }
}
