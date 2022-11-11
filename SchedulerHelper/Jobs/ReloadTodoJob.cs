using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DMR_API.SchedulerHelper.Jobs
{
   public  class ReloadTodoJob : IJob
    {
        HubConnection _connection;
        private readonly IConfiguration _configuration;
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string Signalr_URL = dataMap.GetString("Signalr_URL");

            _connection = new HubConnectionBuilder()
            .WithUrl(Signalr_URL)
            .Build();
            // Loop is here to wait until the server is running
            while (true)
            {

                try
                {
                    await _connection.StartAsync();
                    await _connection.InvokeAsync("JoinReloadTodo");
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
            await Console.Out.WriteLineAsync($"Hub: {_connection.State}");

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var currentDate = DateTime.Now.Date.ToString("MM/dd/yyyy");
                    //var url = $"http://localhost:5003/api/Stir/DeleteAllStirRawdata";
                    var url = $"http://localhost:5003/api/Stir/DeleteAllStirRawdata";
                    //var url = _configuration.GetSection("Appsettings").GetSection("API_RPM_NETWORK").Value;
                    Console.WriteLine($"Starting connect {url}");
                    try
                    {
                        // Thêm header vào HTTP Request
                        HttpResponseMessage response = await httpClient.DeleteAsync(url);

                        // Phát sinh Exception nếu mã trạng thái trả về là lỗi
                        response.EnsureSuccessStatusCode();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Xóa thành công - statusCode {(int)response.StatusCode} {response.ReasonPhrase}");
                            // Đọc thông tin header trả về

                        }
                        else
                        {
                            Console.WriteLine($"Lỗi - statusCode {response.StatusCode} {response.ReasonPhrase}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                await _connection.InvokeAsync("ReloadDispatch");
                await _connection.DisposeAsync();
                Console.ForegroundColor = ConsoleColor.Yellow;
                await Console.Out.WriteLineAsync("Reload Todo");
                await Console.Out.WriteLineAsync($"ReloadTodo ClientHub: {_connection.State}");
                Console.ResetColor();
                await _connection.DisposeAsync();

            }
            catch (Exception)
            {
                await Console.Out.WriteLineAsync("The system can not reload dispatch");
            }
        }
    }
}