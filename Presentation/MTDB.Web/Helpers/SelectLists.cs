using System.Collections.Generic;
using System.Web.Mvc;

namespace MTDB.Helpers
{
    public static class SelectLists
    {
        public static SelectList HeightSelectList
        {
            get
            {
                var heights = new List<string>
                {
                    "5'0\"",
                    "5'1\"",
                    "5'2\"",
                    "5'3\"",
                    "5'4\"",
                    "5'5\"",
                    "5'6\"",
                    "5'7\"",
                    "5'8\"",
                    "5'9\"",
                    "5'10\"",
                    "5'11\"",
                    "6'0\"",
                    "6'1\"",
                    "6'2\"",
                    "6'3\"",
                    "6'4\"",
                    "6'5\"",
                    "6'6\"",
                    "6'7\"",
                    "6'8\"",
                    "6'9\"",
                    "6'10\"",
                    "6'11\"",
                    "7'0\"",
                    "7'1\"",
                    "7'2\"",
                    "7'3\"",
                    "7'4\"",
                    "7'5\"",
                    "7'6\"",
                    "7'7\"",
                    "7'8\"",
                    "7'9\"",
                    "7'10\"",
                    "7'11\""
                };

                return new SelectList(heights);
            }
        }

        public static SelectList PositionsSelectList
        {
            get
            {
                var positions = new List<string>
                {
                    "PG",
                    "SG",
                    "SF",
                    "PF",
                    "C",
                };

                return new SelectList(positions);
            }
        }
    }
}