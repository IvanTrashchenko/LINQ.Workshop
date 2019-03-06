// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01
namespace SampleQueries
{
    using System.Security.Cryptography;
    using System.Windows.Forms.VisualStyles;

    [Title("LINQ Module")]
    [Prefix("Linq")]
    public class LinqSamples : SampleHarness
    {
        private DataSource dataSource = new DataSource();

        [Category("Restriction Operators")]
        [Title("Where - Task 1")]
        [Description("This sample uses the where clause to find all elements of an array with a value less than 5.")]
        public void Linq1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };

            var lowNums = from num in numbers where num < 5 select num;

            Console.WriteLine("Numbers < 5:");
            foreach (var x in lowNums)
            {
                Console.WriteLine(x);
            }
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 2")]
        [Description("This sample return return all presented in market products")]
        public void Linq2()
        {
            var products = from p in this.dataSource.Products where p.UnitsInStock > 0 select p;

            foreach (var p in products)
            {
                ObjectDumper.Write(p);
            }
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 3")]
        [Description("This sample uses the where clause to find all customers from London.")]
        public void Linq3()
        {
            var customers = from p in this.dataSource.Customers where p.City == "London" select p;

            foreach (var p in customers)
            {
                ObjectDumper.Write(p);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 1")]
        [Description("A list of all customers whose total turnover (the sum of all orders) exceeds a certain value X.")]
        public void Linq4()
        {
            int x = 50;

            var customers = from c in this.dataSource.Customers
                            where c.Orders.Sum(o => o.Total) > x
                            select new { Id = c.CustomerID, Sum = c.Orders.Sum(od => od.Total) };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 2a")]
        [Description(
            "A list of suppliers located in the same country and the same city for each customer. Without join.")]
        public void Linq5()
        {
            var suppliers = from c in this.dataSource.Customers
                            from s in this.dataSource.Suppliers
                            where c.Country == s.Country && c.City == s.City
                            select new { c.CustomerID, c.City, c.Country };

            foreach (var s in suppliers)
            {
                ObjectDumper.Write(s);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 2b")]
        [Description("A list of suppliers located in the same country and the same city for each customer. With join.")]
        public void Linq6()
        {
            var suppliers = from c in this.dataSource.Customers
                            join s in this.dataSource.Suppliers on new { c.Country, c.City } equals
                                new { s.Country, s.City }
                            select new { c.CustomerID, c.City, c.Country };

            foreach (var s in suppliers)
            {
                ObjectDumper.Write(s);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 3")]
        [Description("All customers who have orders that exceed the sum of X.")]
        public void Linq7()
        {
            int x = 1000;

            var customers = from c in this.dataSource.Customers
                            where c.Orders.Any(o => o.Total > x)
                            select new { Id = c.CustomerID, Total = c.Orders.Max(o => o.Total) };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 4")]
        [Description(
            "A list of customers with an indication of the beginning of which month of which year they became customers.")]
        public void Linq8()
        {
            var customers = from c in this.dataSource.Customers
                            where c.Orders.Length > 0
                            select new
                            {
                                Id = c.CustomerID,
                                Month = c.Orders.Min(o => o.OrderDate).Month,
                                Year = c.Orders.Min(o => o.OrderDate).Year
                            };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 5")]
        [Description(
            "The list from DQL Task 4 sorted by year, month, customer turnover (from maximum to minimum) and customer name")]
        public void Linq9()
        {
            var customers = from c in this.dataSource.Customers
                            where c.Orders.Length > 0
                            orderby c.Orders.Min(o => o.OrderDate).Month, c.Orders.Min(o => o.OrderDate).Year,
                                c.Orders.Sum(o => o.Total), c.CompanyName descending
                            select new
                            {
                                Id = c.CustomerID,
                                Month = c.Orders.Min(o => o.OrderDate).Month,
                                Year = c.Orders.Min(o => o.OrderDate).Year,
                                Sum = c.Orders.Sum(o => o.Total)
                            };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 6")]
        [Description(
            "A list of customers who have a non-numeric postal code or a region is empty or the operator code is not specified in the phone.")]
        public void Linq10()
        {
            var customers = from c in this.dataSource.Customers
                            where c.PostalCode == null || c.PostalCode.Any(p => !char.IsDigit(p))
                                                       || string.IsNullOrWhiteSpace(c.Region)
                                                       || !c.Phone.StartsWith("(")
                            select new { c.CustomerID, c.PostalCode, c.Region, c.Phone };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 7")]
        [Description("A list of products grouped by categories, inside - by stock availability, within the last group, sorted by cost.")]
        public void Linq11()
        {
            var groups = from p in this.dataSource.Products
                         group p by p.Category
                         into categoryGroup
                         select new
                                    {
                                        Products = from p in categoryGroup
                                                   group p by p.UnitsInStock
                                                   into availabilityGroup
                                                   select new
                                                              {
                                                                  Products = from p in availabilityGroup
                                                                             orderby p.UnitPrice
                                                                             select p
                                                              }
                                    };

            foreach (var categoryGroup in groups)
            {
                foreach (var availabilityGroup in categoryGroup.Products)
                {
                    foreach (var p in availabilityGroup.Products)
                    {
                        ObjectDumper.Write(p);
                    }
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 8")]
        [Description("A list of products grouped into cheap, average price and expensive.")]
        public void Linq12()
        {
            int cheap = 25;
            int average = 50;

            var groups = from p in this.dataSource.Products
                         group p by p.UnitPrice < cheap ? "Cheap" :
                                    p.UnitPrice < average ? "Average price" : "Expensive";

            foreach (var g in groups)
            {
                ObjectDumper.Write(g.Key);
                foreach (var p in g)
                {
                    ObjectDumper.Write(p);
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 9")]
        [Description("Calculation of the average profitability and average intensity of each city.")]
        public void Linq13()
        {
            var cities = from c in this.dataSource.Customers
                         group c by c.City
                         into cityGroup
                         select new
                                    {
                                        City = cityGroup.Key,
                                        Profitability = cityGroup.Average(c => c.Orders.Sum(o => o.Total)),
                                        Intensity = cityGroup.Average(c => c.Orders.Length)
                                    };

            foreach (var c in cities)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 10a")]
        [Description("Average customer activity statistics (Month).")]
        public void Linq14()
        {
            var customers = from c in this.dataSource.Customers
                            select new
                                       {
                                           CustomerId = c.CustomerID,
                                           MonthStat = from o in c.Orders
                                                       group o by o.OrderDate.Month
                                                       into monthGroup
                                                       select new
                                                                  {
                                                                      Month = monthGroup.Key,
                                                                      MonthActivity = monthGroup.Count()
                                                                  }
                                       };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
                foreach (var m in c.MonthStat)
                {
                    ObjectDumper.Write(m);
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 10b")]
        [Description("Average customer activity statistics (Year).")]
        public void Linq15()
        {
            var customers = from c in this.dataSource.Customers
                            select new
                                       {
                                           CustomerId = c.CustomerID,
                                           YearStat = from o in c.Orders
                                                      group o by o.OrderDate.Year
                                                      into yearGroup
                                                      select new
                                                                 {
                                                                     Year = yearGroup.Key,
                                                                     YearActivity = yearGroup.Count()
                                                                 }
                                       };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
                foreach (var y in c.YearStat)
                {
                    ObjectDumper.Write(y);
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("DQL Task 10c")]
        [Description("Average customer activity statistics (Year and month).")]
        public void Linq16()
        {
            var customers = from c in this.dataSource.Customers
                            select new
                                       {
                                           CustomerId = c.CustomerID,
                                           YearMonthStat = from o in c.Orders
                                                           group o by new { o.OrderDate.Year, o.OrderDate.Month }
                                                           into yearMonthGroup
                                                           select new
                                                                      {
                                                                          Year = yearMonthGroup.Key.Year,
                                                                          Month = yearMonthGroup.Key.Month,
                                                                          YearMonthActivity = yearMonthGroup.Count()
                                                                      }
                                       };

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
                foreach (var y in c.YearMonthStat)
                {
                    ObjectDumper.Write(y);
                }
            }
        }
    }
}