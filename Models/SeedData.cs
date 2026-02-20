using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlowPro.Data;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Models
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new FoodieDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<FoodieDbContext>>());

            if (context.Products.Any() || context.Employees.Any() || context.Facilities.Any() || context.Orders.Any())
            {
                return;
            }

            var employees = BuildEmployees();
            context.Employees.AddRange(employees);

            var facilities = BuildFacilities();
            context.Facilities.AddRange(facilities);

            var products = BuildProducts();
            context.Products.AddRange(products);

            await context.SaveChangesAsync();

            var orders = BuildOrders(facilities, products, employees);
            context.Orders.AddRange(orders);
            context.OrderItems.AddRange(orders.SelectMany(o => o.OrderItems));
            context.OrderProcessings.AddRange(orders.Select(o => o.OrderProcessing));

            await context.SaveChangesAsync();

            if (context.Products.Count() != 100) throw new Exception("Expected exactly 100 products.");
            if (context.Employees.Count() != 20) throw new Exception("Expected exactly 20 employees.");
            if (context.Facilities.Count() != 30) throw new Exception("Expected exactly 30 facilities.");
            if (context.Orders.Count() != 30) throw new Exception("Expected exactly 30 orders.");
            if (!context.Products.Any(p => p.QuantityInStock == 0)) throw new Exception("Missing zero-stock product example.");
            if (context.Orders.Count(o => o.OrderStatus == OrderStatus.Created) == 0) throw new Exception("Missing Created orders.");
            if (context.Orders.Count(o => o.OrderStatus == OrderStatus.Prepared) == 0) throw new Exception("Missing Prepared orders.");
            if (context.Orders.Count(o => o.OrderStatus == OrderStatus.Scanned) == 0) throw new Exception("Missing Scanned orders.");
            if (context.Orders.Count(o => o.OrderStatus == OrderStatus.Delivered) == 0) throw new Exception("Missing Delivered orders.");
            if (!context.Orders.Any(o => o.OrderItems.Count > 1)) throw new Exception("Missing multi-line order example.");
        }

        private static List<Employee> BuildEmployees()
        {
            var names = new[]
            {
                "Adrian Dimitrov Ganchev",
                "Aleks Evgeni Gospodinov",
                "Aleksandar Dimitrov Ivanov",
                "Alik Sarko Baltayan",
                "Antoniya Krasimirova Miteva",
                "Atanas Nikolov Nikolov",
                "Bilyana Todorova Bachvarova",
                "Borislav Ivaylov Yamandiev",
                "Valentin Stefanov Berberov",
                "Veneta Toshkova Koleva",
                "Venko Oleg Andreev",
                "Georgi Georgiev Mihaylov",
                "Darina Krasimirova Ilieva",
                "Ivaylo Karapenchev",
                "Yoan Yanev Vasilev",
                "Kalina Todorova Todorova",
                "Kristiyan Atanasov Atanasov",
                "Mihail Zhivkov Stamatov",
                "Nevena Ivanova Gospodinova-Dimitrova",
                "Teodor Vitaliev Rodyukov"
            };

            var positions = new[] { "Office", "Scanner", "Driver", "Packer" };

            var list = new List<Employee>();
            for (int i = 0; i < 20; i++)
            {
                list.Add(new Employee
                {
                    FullName = names[i],
                    Position = positions[i % positions.Length],
                    Phone = $"+359 88{(i + 10):D2} {((i + 1234) % 1000):D3} {(i * 37 + 555) % 1000:D3}",
                    IsActive = true
                });
            }
            return list;
        }

        private static List<Facility> BuildFacilities()
        {
            var entries = new (string Name, string City, string Area, string Rep)[]
            {
                ("Sofia DC Druzhba","Sofia","Druzhba","Kaloyan Petrov"),
                ("Sofia Store Lozenets","Sofia","Lozenets","Irena Nikolova"),
                ("Sofia Cross-Dock Iliyantsi","Sofia","Iliyantsi","Dimitar Yanev"),
                ("Plovdiv DC Trakia","Plovdiv","Trakia","Raya Krasteva"),
                ("Plovdiv Store Kapana","Plovdiv","Kapana","Stefan Kolev"),
                ("Varna DC Asparuhovo","Varna","Asparuhovo","Petar Hristov"),
                ("Varna Store Seaside","Varna","Primorski","Mila Stoyanova"),
                ("Burgas DC Izgrev","Burgas","Izgrev","Tanya Georgieva"),
                ("Ruse Store Center","Ruse","Center","Veselin Dimov"),
                ("Stara Zagora Depot","Stara Zagora","Tri Chuchura","Svetla Ivanova"),
                ("Pleven Store Panorama","Pleven","Panorama","Valeri Stoyanov"),
                ("Veliko Tarnovo Store Tsarevets","Veliko Tarnovo","Tsarevets","Miroslav Iliev"),
                ("Blagoevgrad Store Varosha","Blagoevgrad","Varosha","Nadezhda Petkova"),
                ("Vidin Store Danube","Vidin","Dunavska","Plamen Dimitrov"),
                ("Montana Depot","Montana","Zhivovtsi","Siyana Angelova"),
                ("Shumen Store Center","Shumen","Center","Hristo Georgiev"),
                ("Haskovo DC","Haskovo","Orfey","Yoana Yordanova"),
                ("Yambol Store Bezisten","Yambol","Bezisten","Stoyan Kolarov"),
                ("Sliven DC","Sliven","Rechitsa","Dafina Koleva"),
                ("Dobrich Store","Dobrich","Center","Kalin Vasilev"),
                ("Vratsa Depot","Vratsa","Dabnika","Tsvetelina Borisova"),
                ("Kyustendil Store","Kyustendil","Center","Andrey Marinov"),
                ("Pernik Store","Pernik","Tsurkva","Simeon Petrov"),
                ("Pazardzhik Depot","Pazardzhik","Iztok","Mariya Nikolaeva"),
                ("Smolyan Store","Smolyan","Ustovo","Kristina Ivanova"),
                ("Kardzhali Depot","Kardzhali","Veslets","Boris Atanasov"),
                ("Lovech Store","Lovech","Varosha","Rosen Georgiev"),
                ("Silistra Store","Silistra","Dobrudzha","Albena Hristova"),
                ("Targovishte Depot","Targovishte","Varosha","Galina Dimitrova"),
                ("Razgrad Store","Razgrad","Center","Ivailo Stoyanov")
            };

            var list = new List<Facility>();
            int idx = 0;
            foreach (var e in entries)
            {
                idx++;
                list.Add(new Facility
                {
                    Name = e.Name,
                    Address = $"{e.City}, {e.Area} {idx}",
                    Phone = $"+359 2 {(idx * 111) % 1000:D3} {((idx * 321) + 100) % 1000:D3}",
                    Area = e.Area,
                    RepresentativeName = e.Rep,
                    IsActive = true
                });
            }
            return list;
        }

        private static List<Product> BuildProducts()
        {
            var productNames = new[]
            {
                "Kiselo mlyako","Sirene","Kashkaval","Lutenitsa","Lyutenitsa spicy","Ajvar","Lukanka","Sudzhuk","Banitsa sheets","Boza",
                "Tarator mix","Shopska salad kit","Lyutenica premium","Kebapche","Kyufte","Feta sheep","Yogurt drink","Mlin","Yufka","Ayran",
                "Pita bread","Pitka","Medenki","Lokum","Halva","Lyutenica rustic","Rakia crackers","Rose jam","Pepper burek","Lyutenica kids"
            };
            var brands = new[]
            {
                "Kamenitza Food","Zagorka Foods","Devin Dairy","Harmonica","Balkan Taste","Rodopa","Sofia Mel","Billa BG","Kaufland BG","Lactima BG"
            };

            var products = new List<Product>();
            int rn = 0;
            for (int i = 0; i < productNames.Length; i++)
            {
                for (int j = 0; j < brands.Length; j++)
                {
                    if (products.Count == 100) break;
                    rn++;
                    var price = 1.00m + ((rn % 50) * 0.50m);
                    var qty = (rn % 10 == 0) ? 0 : ((rn % 20) + 5);
                    products.Add(new Product
                    {
                        Name = productNames[i],
                        Brand = brands[j],
                        Price = price,
                        QuantityInStock = qty,
                        MinimumStockLevel = (rn % 5) + 3
                    });
                }
                if (products.Count == 100) break;
            }
            return products;
        }

        private static List<Order> BuildOrders(List<Facility> facilities, List<Product> products, List<Employee> employees)
        {
            var orders = new List<Order>();
            for (int n = 1; n <= 30; n++)
            {
                var status = (n % 4) switch
                {
                    1 => OrderStatus.Created,
                    2 => OrderStatus.Prepared,
                    3 => OrderStatus.Scanned,
                    _ => OrderStatus.Delivered
                };

                var facility = facilities[(n - 1) % facilities.Count];
                var order = new Order
                {
                    FacilityId = facility.Id,
                    OrderStatus = status,
                    CreatedAt = DateTime.UtcNow.AddDays(-n),
                    OrderItems = new List<OrderItem>()
                };

                var lines = 2 + (n % 3);
                for (int ln = 1; ln <= lines; ln++)
                {
                    var productIndex = ((n * 7) + (ln * 3)) % products.Count;
                    var product = products[productIndex];
                    var qty = ((n + ln) % 5) + 1;
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = qty,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * qty
                    });
                }

                if (n % 5 == 0 && order.OrderItems.Any())
                {
                    var first = order.OrderItems.First();
                    if (first.Quantity > 1)
                    {
                        var original = first.Quantity;
                        var half = original / 2;
                        var remainder = original - half;
                        first.Quantity = half;
                        first.TotalPrice = first.UnitPrice * half;
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = first.ProductId,
                            Quantity = remainder,
                            UnitPrice = first.UnitPrice,
                            TotalPrice = first.UnitPrice * remainder
                        });
                    }
                    else
                    {
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = first.ProductId,
                            Quantity = 1,
                            UnitPrice = first.UnitPrice,
                            TotalPrice = first.UnitPrice * 1
                        });
                    }
                }

                order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

                var createdBy = employees[(n + 1) % employees.Count];
                var preparedBy = employees[(n + 3) % employees.Count];
                var scannedBy = employees[(n + 5) % employees.Count];
                order.OrderProcessing = new OrderProcessing
                {
                    CreatedByEmployeeId = createdBy.Id,
                    PreparedByEmployeeId = status >= OrderStatus.Prepared ? preparedBy.Id : null,
                    ScannedByEmployeeId = status >= OrderStatus.Scanned ? scannedBy.Id : null,
                    ProcessDate = DateTime.UtcNow.AddDays(-n).AddHours(1 + (n % 8))
                };

                orders.Add(order);
            }

            return orders;
        }
    }
}
