using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Services
{
	public interface ISettingsService
	{
		Task<ReaderSettings> GetSettings();
		void SetSettings( ReaderSettings settings );
        ReaderSettingsViewModel.DeffaultSettingsType DeffaultSettings { get; set; }
	}
}
