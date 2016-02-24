/*
 * Author: Vitaly Leschenko, CactusSoft (http://cactussoft.biz/), 2013
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
 * 02110-1301, USA.
 */

using System;

namespace BookParser.Models
{  
  
    public class BookModel : BaseTable
    {
      
        public string BookID { get; set; }

       
        public string Type { get; set; }

      
        public string Title { get; set; }

     
        public string Author { get; set; }

        [Obsolete]
        public int AuthorHash { get; set; }

        
        public int CurrentTokenID { get; set; }

       
        public int TokenCount { get; set; }

       
        public bool Hidden { get; set; }

       
        public bool Trial { get; set; }

       
        public long LastUsage { get; set; }

       
        public int? WordCount { get; set; }

       
        public bool Deleted { get; set; }

       
        public long? CreatedDate { get; set; }

       
        public string UniqueID { get; set; }

       
        public string Url { get; set; }

      
        public bool IsFavourite { get; set; }

     
        public string CatalogItemId { get; set; }

      
        public string Language { get; set; }

     
        public string Description { get; set; }
    }
}