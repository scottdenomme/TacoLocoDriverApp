using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TacoLocoDriverApp.Api.Controllers;
using TacoLocoDriverApp.Api.Models;
using Xunit;

namespace TacoLocoDriverApp.Tests
{
    public class DeliveriesControllerTest
    {
        private readonly ILogger<DeliveriesController> _logger;
        private readonly DeliveriesController _deliveriesController;
        private string _connectionString;

        public  Delivery _testDelivery = new Delivery()
        {
            Customer = new Customer()
            {
                FirstName = "TEST",
                LastName = "CUSTOMER",
                StreetAddress = "1345 Maplelawn St.",
                City = "Farmington",
                State = "MI",
                Zip = "48163"
            }
        };

        public DeliveriesControllerTest()
        {
            _deliveriesController = new DeliveriesController(_logger);
            _connectionString = System.IO.Path.GetFullPath("./../../../..") + "\\Databases\\TacoLocoDb.db";
        }
        
        public bool CleanupDbData()
        {
            try
            {
                int customerId = 0;

                using (var connection = new SqliteConnection("Data Source=" + _connectionString))
                {
                    connection.Open();
                    
                    var getTestCustoemrIdQuery = connection.CreateCommand();
                    getTestCustoemrIdQuery.CommandText = @"
                            SELECT Id 
                            FROM Customer
                            WHERE FirstName = 'TEST' AND LastName = 'CUSTOMER' 
                        ";

                    using (var reader = getTestCustoemrIdQuery.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read() && count < 1)
                        {
                            customerId = int.Parse(reader.GetString(0));
                            count++;
                        }
                    }

                    var cleanupDeliveryCommand = connection.CreateCommand();
                    cleanupDeliveryCommand.CommandText = @"
                            DELETE FROM Delivery
                            WHERE CustomerId = $customerId
                        ";
                    cleanupDeliveryCommand.Parameters.AddWithValue("$customerId", customerId);
                    cleanupDeliveryCommand.ExecuteNonQuery();

                    var cleanupCustomerCommand = connection.CreateCommand();
                    cleanupCustomerCommand.CommandText = @"
                            DELETE FROM Customer
                            WHERE Id = $customerId
                        ";
                    cleanupCustomerCommand.Parameters.AddWithValue("$customerId", customerId);
                    cleanupCustomerCommand.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public List<int> CreateDbData()
        {
            List<int> testIds = new List<int>();
            int testCustomerId = 0;
            int testDeliveryId = 0;

            try
            {
                using (var connection = new SqliteConnection("Data Source=" + _connectionString))
                {
                    

                    connection.Open();

                    var createTestCustomerCommand = connection.CreateCommand();
                    createTestCustomerCommand.CommandText = @"
                            INSERT INTO Customer (FirstName, LastName, StreetAddress, City, State, Zip)
                            VALUES ('TEST', 'CUSTOMER', '1345 Maplelawn St.', 'Farmington', 'MI', '48150')
                        ";
                    createTestCustomerCommand.ExecuteNonQuery();

                    var getTestCustomerIdQuery = connection.CreateCommand();
                    getTestCustomerIdQuery.CommandText = @"
                            SELECT Id FROM Customer
                            WHERE FirstName = 'TEST' AND LastName = 'CUSTOMER' 
                            ORDER BY Id DESC
                        ";
                    using (var reader = getTestCustomerIdQuery.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read() && count < 1)
                        {
                            testCustomerId = int.Parse(reader.GetString(0));
                            count++;
                        }
                    }

                    var createTestDeliveryCommand = connection.CreateCommand();
                    createTestDeliveryCommand.CommandText = @"
                            INSERT INTO Delivery (CustomerId)
                            VALUES ($testCustomerId)
                        ";

                    createTestDeliveryCommand.Parameters.AddWithValue("$testCustomerId", testCustomerId);
                    createTestDeliveryCommand.ExecuteNonQuery();

                    var getTestDeliveryIdCommand = connection.CreateCommand();
                    getTestDeliveryIdCommand.CommandText = @"
                            SELECT Id FROM Delivery
                            WHERE CustomerId = $testCustomerId
                        ";
                    getTestDeliveryIdCommand.Parameters.AddWithValue("$testCustomerId", testCustomerId);
                    using (var reader = getTestDeliveryIdCommand.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read() && count < 1)
                        {
                            testDeliveryId = int.Parse(reader.GetString(0));
                            count++;
                        }
                    }

                    testIds.Add(testCustomerId);
                    testIds.Add(testDeliveryId);

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return testIds;
            }

            return testIds;
        }

        [Fact]
        public void GetDeliveriesSucess()
        {
            List<Delivery> actionResult = _deliveriesController.Get().ToList();
            Assert.False(actionResult.Count == 0);
        }

        [Fact]
        public void CreateDeliverySuccess()
        {
            List<Delivery> originalListResult = _deliveriesController.Get().ToList();
            int originalCount = originalListResult.Count;

            string response = _deliveriesController.AddDelivery(_testDelivery.Customer.FirstName, _testDelivery.Customer.LastName, 
                    _testDelivery.Customer.StreetAddress, _testDelivery.Customer.City, _testDelivery.Customer.State, _testDelivery.Customer.Zip);

            List<Delivery> newListResult = _deliveriesController.Get().ToList();
            int newCount = newListResult.Count;

            Assert.Equal(originalCount, newCount - 1);
            Assert.Equal(newListResult[newCount - 1].Customer.FirstName, _testDelivery.Customer.FirstName);
            Assert.Equal(newListResult[newCount - 1].Customer.LastName, _testDelivery.Customer.LastName);
            Assert.Equal(newListResult[newCount - 1].Customer.StreetAddress, _testDelivery.Customer.StreetAddress);
            Assert.Equal(newListResult[newCount - 1].Customer.City, _testDelivery.Customer.City);
            Assert.Equal(newListResult[newCount - 1].Customer.State, _testDelivery.Customer.State);
            Assert.Equal(newListResult[newCount - 1].Customer.Zip, _testDelivery.Customer.Zip);

            bool cleanupSuccess = CleanupDbData();

            Assert.True(cleanupSuccess);
        }

        [Fact]
        public void UpdateCustomerSuccess()
        {
            List<int> testIds = CreateDbData();
            Assert.NotEmpty(testIds);

            string originalLastName = "";
            string newLastName = "";
            using (var connection = new SqliteConnection("Data Source=" + _connectionString))
            {
                connection.Open();
                var getTestCustomerDataQuery = connection.CreateCommand();
                getTestCustomerDataQuery.CommandText = @"
                            SELECT LastName FROM Customer
                            WHERE Id = $customerId 
                        ";
                getTestCustomerDataQuery.Parameters.AddWithValue("$customerId", testIds[0]);
                using (var reader = getTestCustomerDataQuery.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read() && count < 1)
                    {
                        originalLastName = reader.GetString(0);
                        count++;
                    }
                }


                _deliveriesController.UpdateCustomer(testIds[1], default, default, "CUSTOMER_2", default, default, default, default);
                var getUpdatedTestCustomerDataQuery = connection.CreateCommand();
                getUpdatedTestCustomerDataQuery.CommandText = @"
                            SELECT LastName FROM Customer
                            WHERE Id = $customerId 
                        ";
                getUpdatedTestCustomerDataQuery.Parameters.AddWithValue("$customerId", testIds[0]);
                using (var reader = getUpdatedTestCustomerDataQuery.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read() && count < 1)
                    {
                        newLastName = reader.GetString(0);
                        count++;
                    }
                }


                Assert.NotEqual(originalLastName, newLastName);

                _deliveriesController.UpdateCustomer(testIds[1], default, default, "CUSTOMER");

                var getResetTestCustomerDataQuery = connection.CreateCommand();
                getResetTestCustomerDataQuery.CommandText = @"
                            SELECT LastName FROM Customer
                            WHERE Id = $customerId 
                        ";
                getResetTestCustomerDataQuery.Parameters.AddWithValue("$customerId", testIds[0]);
                using (var reader = getResetTestCustomerDataQuery.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read() && count < 1)
                    {
                        newLastName = reader.GetString(0);
                        count++;
                    }
                }
                connection.Close();
            }

            Assert.Equal(originalLastName, newLastName);
            bool cleanup = CleanupDbData();
            Assert.True(cleanup);
        }

        [Fact]
        public void DeleteShipmentSuccess()
        {
            List<int> testIds = CreateDbData();
            Assert.NotEmpty(testIds);

            int originalCount = _deliveriesController.Get().ToList().Count;

            _deliveriesController.DeleteDelivery(testIds[1]);

            int currentCount = _deliveriesController.Get().ToList().Count;

            Assert.Equal(originalCount, currentCount + 1);
        }
    }
}
