using AsyncPlayground.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AsyncPlayground
{
    internal class Application
    {
        private ConcurrentDictionary<string, int> _cache = new() { ["x"] = 42 };
        private readonly ApplicationContext _context;

        public Application(ApplicationContext context)
        {
            _context = context;
        }

        public async Task Go()
        {
            Task1_HelloWorldAsync();
            await Task2_ReturnTaskToAddEmployeeAsync();
            await Task3_Exception();
            await Task4_ReturnEmployees();
            await Task5_Disposal();
            await Task6_MultipleCalls();
            Task7_1_GetFirstEmployeeName();
            Task7_2_CorrectBlocking();
            Task8_FireAndForget();
            await Task9_GetCachedValueAsync();

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var task = Task10_Cancellation(token);
            cancellationTokenSource.Cancel();
        }

        // Task 1. Unnecessary State Machine involved.
        private void Task1_HelloWorldAsync()
        {
            Console.WriteLine("Hello World!");
        }

        // Task 2. Everything looks fine... or does it?
        private async Task<int> Task2_ReturnTaskToAddEmployeeAsync()
        {
            try
            {
                return await CreateEmployee();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private Task<int> CreateEmployee()
        {
            // Add a new employee to the database
            var employee = new Employee
            {
                Name = "John Doe",
                Department = "IT"
            };
            _context.Employees.Add(employee);
            return _context.SaveChangesAsync();
        }

        // Task 3. We should see the exception message in the output
        private async Task Task3_Exception()
        {
            await CatchTheException();
        }

        private async Task AsyncVoidMethodThrowsException()
        {
            await Task.Delay(100);
            throw new Exception("Hmmm, something went wrong!");
        }

        public async Task CatchTheException()
        {
            try
            {
                await AsyncVoidMethodThrowsException();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // Task 4. Return the list of employees
        private Task<List<Employee>> Task4_ReturnEmployees()
        {
            return ReturnEmployeesListAsync();
        }

        private async Task<List<Employee>> ReturnEmployeesListAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        // Task 5. Avoid early disposal
        private async Task Task5_Disposal()
        {
            var result = await ReturnTaskToReadFileEarlyDisposeAsync();
            Console.WriteLine("Task5 " + result);
        }

        public async Task<string> ReturnTaskToReadFileEarlyDisposeAsync()
        {
            using (var reader = new StreamReader("config.json"))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // Task 6. Optimize multiple calls that are not dependent on each other
        private async Task<List<Employee>> Task6_MultipleCalls()
        {
            var employeesFromDepartment1 = GetEmployeesFromDepartmentAsync("IT");
            var employeesFromDepartment2 = GetEmployeesFromDepartmentAsync("Financial");
            var employeesFromDepartment3 = GetEmployeesFromDepartmentAsync("BI");


            await Task.WhenAll(employeesFromDepartment1, employeesFromDepartment2, employeesFromDepartment3);

            var result = new List<Employee>();
            result.AddRange(employeesFromDepartment1.Result);
            result.AddRange(employeesFromDepartment2.Result);
            result.AddRange(employeesFromDepartment3.Result);

            return result;
        }

        private async Task<List<Employee>> GetEmployeesFromDepartmentAsync(string department)
        {
            return await _context.Employees.Where(e => e.Department == department).ToListAsync();
        }

        // Task 7.1. I just don't like AggregateExceptions
        private string Task7_1_GetFirstEmployeeName()
        {
            return GetFirstEmployeeNameAsync().GetAwaiter().GetResult();
        }

        private async Task<string> GetFirstEmployeeNameAsync()
        {
            return (await _context.Employees.FirstOrDefaultAsync()).Name;
        }

        // Task 7.2. I just don't like AggregateExceptions
        private void Task7_2_CorrectBlocking()
        {
            LongImportantJobThatShouldBeAwaited().GetAwaiter().GetResult();
        }

        private async Task LongImportantJobThatShouldBeAwaited()
        {
            await Task.Delay(5000);
        }

        // Task 8. Avoid Fire-and-Forget Without Logging or Handling
        private async Task Task8_FireAndForget()
        {
            try
            {
                await DoBackgroundWorkAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DoBackgroundWorkAsync()
        {
            await Task.Delay(1000);
            Console.WriteLine("Background work completed!");
            throw new Exception("Background work failed!");
        }

        // Task 9: This method is being called really often. Try to optimize its memory consumption.
        public async Task<int> Task9_GetCachedValueAsync()
        {
            if (_cache.TryGetValue("x", out var value))
                return value;

            return await FetchValueAsync();
        }

        private async Task<int> FetchValueAsync()
        {
            await Task.Delay(100);

            _cache["x"] = new Random().Next();

            return _cache["x"];
        }

        // Task 10. Provide cancellation mechanism for the long-running task
        private async Task Task10_Cancellation(CancellationToken cancellationToken)
        {
            try
            {
                await LongRunningTaskAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation if needed, such as logging or cleanup
                Console.WriteLine("Task was cancelled.");
            }
        }

        public async Task LongRunningTaskAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 100; i++)
            {
                // Check if cancellation has been requested
                cancellationToken.ThrowIfCancellationRequested();

                // Simulate an async call that takes some time to complete
                await Task.Delay(1000);
            }
        }
    }
}
