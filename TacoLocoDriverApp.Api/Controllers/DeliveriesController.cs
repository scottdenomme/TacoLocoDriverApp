using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TacoLocoDriverApp.Api.Models;

namespace TacoLocoDriverApp.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeliveriesController : ControllerBase
    {
        private readonly ILogger<DeliveriesController> _logger;
        private string _connectionString;
        private SqliteConnectionStringBuilder _connectionStringBuilder = new SqliteConnectionStringBuilder();
         
        public DeliveriesController(ILogger<DeliveriesController> logger)
        {
            _logger = logger;
            _connectionStringBuilder.DataSource = "../Databases/TacoLocoDb.db";
            string dbPathPrefix = System.IO.Path.GetFullPath("./..");
            if (System.IO.Path.GetFullPath("./..").Contains("TacoLocoDriverApp.Tests"))
            {
                _connectionString = System.IO.Path.GetFullPath("./../../../..") + "\\Databases\\TacoLocoDb.db";
            }
            else
            {
                _connectionString = System.IO.Path.GetFullPath("./..") + "\\Databases\\TacoLocoDb.db";
            }
        }

        [HttpGet]
        [Route("/deliveries")]
        public IEnumerable<Delivery> Get()
        {
            List<Delivery> deliveries = new List<Delivery>();

            using (var connection = new SqliteConnection("Data Source=" + _connectionString))
            {
                connection.Open();
            
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT D.Id, C.Id, C.FirstName, C.LastName, C.StreetAddress, C.City, C.State, C.Zip 
                    FROM Delivery D
                    JOIN Customer C ON D.CustomerId = C.Id
                ";
            
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(1);
                        deliveries.Add(new Delivery
                        {
                            Id = int.Parse(reader.GetString(0)),
                            Customer = new Customer()
                            {
                                Id = int.Parse(reader.GetString(1)),
                                FirstName = reader.GetString(2),
                                LastName = reader.GetString(3),
                                StreetAddress = reader.GetString(4),
                                City = reader.GetString(5),
                                State = reader.GetString(6),
                                Zip = reader.GetString(7)
                            }
                        });
                    }
                }
            }
            return deliveries;
        }

        [HttpPost]
        [Route("/update-customer")]
        public string UpdateCustomer(int deliveryId, int customerId = 0, string firstName = null, string lastName = null, string streetAddress = null, string city = null, string state = null, string zip = null)
        {
            Delivery updateDelivery = new Delivery();
            using (var connection = new SqliteConnection("Data Source=" + _connectionString))
            {
                connection.Open();

                if (customerId != 0)
                {
                    var getNewCustomerQuery = connection.CreateCommand();
                    getNewCustomerQuery.CommandText = @"
                        SELECT Id, FirstName, LastName, StreetAddress, City, State, Zip
                        FROM Customer
                        WHERE Id = $customerId
                    ";
                    getNewCustomerQuery.Parameters.AddWithValue("$customerId", customerId);

                    using (var reader = getNewCustomerQuery.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            updateDelivery.Id = deliveryId;
                            updateDelivery.Customer = new Customer()
                            {
                                Id = (customerId != 0) ? customerId : int.Parse(reader.GetString(0)),
                                FirstName = firstName ?? reader.GetString(1),
                                LastName = lastName ?? reader.GetString(2),
                                StreetAddress = streetAddress ?? reader.GetString(3),
                                City = city ?? reader.GetString(4),
                                State = state ?? reader.GetString(5),
                                Zip = zip ?? reader.GetString(6)
                            };
                        }
                    }

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                            UPDATE Delivery
                            SET CustomerId = $customerId
                            WHERE Id = $deliveryId
                        ";
                    command.Parameters.AddWithValue("$customerId", updateDelivery.Customer.Id);
                    command.Parameters.AddWithValue("$deliveryId", updateDelivery.Id);

                    command.ExecuteNonQuery();
                }
                else
                {
                    var getDeliveryCustomerCommand = connection.CreateCommand();
                    getDeliveryCustomerCommand.CommandText = @"
                            SELECT D.Id, C.Id, C.FirstName, C.LastName, C.StreetAddress, C.City, C.State, C.Zip
                            FROM Customer C
                            JOIN Delivery D
                            ON C.Id = D.CustomerId
                            WHERE D.Id = $deliveryId
                        ";
                    getDeliveryCustomerCommand.Parameters.AddWithValue("$deliveryId", deliveryId);
                    using (var reader = getDeliveryCustomerCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            updateDelivery.Id = int.Parse(reader.GetString(0));
                            updateDelivery.Customer = new Customer()
                            {
                                Id = (customerId != 0) ? customerId : int.Parse(reader.GetString(1)),
                                FirstName = firstName ?? reader.GetString(2),
                                LastName = lastName ?? reader.GetString(3),
                                StreetAddress = streetAddress ?? reader.GetString(4),
                                City = city ?? reader.GetString(5),
                                State = state ?? reader.GetString(6),
                                Zip = zip ?? reader.GetString(7)
                            };
                        }
                    }

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                            UPDATE Customer
                            SET FirstName = $firstName, LastName = $lastName, StreetAddress = $streetAddress, City = $city, State = $state, Zip = $zip
                            WHERE Id = $customerId
                        ";
                    command.Parameters.AddWithValue("$firstName", updateDelivery.Customer.FirstName);
                    command.Parameters.AddWithValue("$lastName", updateDelivery.Customer.LastName);
                    command.Parameters.AddWithValue("$streetAddress", updateDelivery.Customer.StreetAddress);
                    command.Parameters.AddWithValue("$city", updateDelivery.Customer.City);
                    command.Parameters.AddWithValue("$state", updateDelivery.Customer.State);
                    command.Parameters.AddWithValue("$zip", updateDelivery.Customer.Zip);
                    command.Parameters.AddWithValue("$customerId", updateDelivery.Customer.Id);

                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        
            string json = JsonConvert.SerializeObject(updateDelivery.Customer);
            return json;
        }

        [HttpPost]
        [Route("/add-delivery")]
        public string AddDelivery(string firstName = null, string lastName = null, string streetAddress = null, string city = null, string state = null, string zip = null)
        {
            Delivery newDelivery = new Delivery()
            {
                Customer = new Customer()
                {
                    FirstName = firstName,
                    LastName = lastName,
                    StreetAddress = streetAddress,
                    City = city,
                    State = state,
                    Zip = zip
                }
            };

            using (var connection = new SqliteConnection("Data Source=" + _connectionString))
            {
                connection.Open();

                var insertCustomerCommand = connection.CreateCommand();
                insertCustomerCommand.CommandText = @"
                    INSERT INTO Customer (FirstName, LastName, StreetAddress, City, State, Zip)
                    VALUES ($firstName, $lastName, $streetAddress, $city, $state, $zip)
                ";
                insertCustomerCommand.Parameters.AddWithValue("$firstName", newDelivery.Customer.FirstName);
                insertCustomerCommand.Parameters.AddWithValue("$lastName", newDelivery.Customer.LastName);
                insertCustomerCommand.Parameters.AddWithValue("$streetAddress", newDelivery.Customer.StreetAddress);
                insertCustomerCommand.Parameters.AddWithValue("$city", newDelivery.Customer.City);
                insertCustomerCommand.Parameters.AddWithValue("$state", newDelivery.Customer.State);
                insertCustomerCommand.Parameters.AddWithValue("$zip", newDelivery.Customer.Zip);

                insertCustomerCommand.ExecuteNonQuery();

                var getNewCustomerIdQuery = connection.CreateCommand();
                getNewCustomerIdQuery.CommandText = @"
                    SELECT Id FROM Customer ORDER BY Id DESC
                ";

                using (var reader = getNewCustomerIdQuery.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read() && count < 1)
                    {
                        newDelivery.Customer.Id = int.Parse(reader.GetString(0));
                        count++;
                    }
                }

                var insertDeliveryCommand = connection.CreateCommand();

                insertDeliveryCommand.CommandText = @"
                    INSERT INTO Delivery (CustomerId)
                    VALUES ($customerId)
                ";
                insertDeliveryCommand.Parameters.AddWithValue("$customerId", newDelivery.Customer.Id);

                insertDeliveryCommand.ExecuteNonQuery();

                var getNewDeliveryIdCommand = connection.CreateCommand();
                getNewDeliveryIdCommand.CommandText = @"
                    SELECT Id FROM Delivery ORDER BY Id DESC
                ";

                //Would usually not use this method due to other orders maybe being added to system.. would usually use an output from the insert to get the 
                //Latest inserted customer and delivery ids to use.
                using (var reader = getNewDeliveryIdCommand.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read() && count < 1)
                    {
                        newDelivery.Id = int.Parse(reader.GetString(0));
                        count++;
                    }
                }

                connection.Close();
            }

            string json = JsonConvert.SerializeObject(newDelivery);
            return json;
        }

        [HttpPost]
        [Route("/delete-delivery")]
        public string DeleteDelivery(int deliveryId)
        {
            Delivery deletedDelivery = new Delivery()
            {
                Id = deliveryId,
                Customer = new Customer()
            };

            using (var connection = new SqliteConnection("Data Source=" + _connectionString))
            {
                connection.Open();

                var getDeliveryCustomerCommand = connection.CreateCommand();
                getDeliveryCustomerCommand.CommandText = @"
                    SELECT D.Id, C.Id, C.FirstName, C.LastName, C.StreetAddress, C.City, C.State, C.Zip
                    FROM Customer C
                    JOIN Delivery D
                    ON C.Id = D.CustomerId
                    WHERE D.Id = $deliveryId
                ";
                getDeliveryCustomerCommand.Parameters.AddWithValue("$deliveryId", deliveryId);
                using (var reader = getDeliveryCustomerCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deletedDelivery.Id = int.Parse(reader.GetString(0));
                        deletedDelivery.Customer = new Customer()
                        {
                            Id = int.Parse(reader.GetString(1)),
                            FirstName = reader.GetString(2),
                            LastName = reader.GetString(3),
                            StreetAddress = reader.GetString(4),
                            City = reader.GetString(5),
                            State = reader.GetString(6),
                            Zip = reader.GetString(7)
                        };
                    }
                }

                var deleteDeliveryCommand = connection.CreateCommand();
                deleteDeliveryCommand.CommandText = @"
                    DELETE FROM Delivery WHERE Id = $deliveryId
                ";
                deleteDeliveryCommand.Parameters.AddWithValue("$deliveryId", deliveryId);
                deleteDeliveryCommand.ExecuteNonQuery();

                var deleteCustomerCommand = connection.CreateCommand();
                deleteCustomerCommand.CommandText = @"
                    DELETE FROM Customer WHERE Id = $customerId
                ";
                deleteCustomerCommand.Parameters.AddWithValue("$customerId", deletedDelivery.Customer.Id);
                deleteCustomerCommand.ExecuteNonQuery();

                connection.Close();
            }

            string json = JsonConvert.SerializeObject(deletedDelivery);
            return json;
        }
    }
}
