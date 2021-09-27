#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.IO;
using System.Threading.Tasks;
using Greet;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace Client
{
    public class Program
    {
        static string _bearerTokenFile = "BearerToken.txt";
        public static IConfiguration? configuration;

        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);

            // Read Bearer token from BearerToken.txt, if present 
            string token = string.Empty;
            if (!File.Exists(_bearerTokenFile))
            {
                // Use Device Codeflow to get accesstoken
                IConfiguration configuration = GetAppConfiguration();
                var deviceCode = new DeviceCodeAuthProvider(configuration);
                token = await deviceCode.GetAccessToken(
                    new string[] { configuration.GetSection("scope").Value });

                Console.WriteLine($"token: {token}");
            }
            else
            {
                // Read access token from BearerToken.txt file
                // 
                Console.WriteLine
                    ($"Reading token file: {_bearerTokenFile}");
                token = File.ReadAllText(_bearerTokenFile);
            }

            // Case 1: bug is gRPC 
            // Header name: authorization (Lowercase letters)
            // gRpc Service doesn't recognize token as authorization tken

            // Case 2: bug is gRPC 
            // Header name: Authorization (First letter Uppercase letter)
            // gRpc Service does process the token

            Metadata metadata = new Metadata
            {
                { "authorization", $"Bearer {token}"}
            };

            var reply = await client.SayHelloAsync(
                request: new HelloRequest { Name = "GreeterClient" },
                headers: metadata);
            Console.WriteLine("Greeting: " + reply.Message);

            Console.WriteLine("Shutting down");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static IConfiguration GetAppConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }
    }
}
