using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.KitchenSink.OptionParsing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WireRabbit
{
    class Program
    {
        /// <summary>
        /// Application options
        /// </summary>
        private static AppOptions _options;

        public static void HandleMessage(BasicDeliverEventArgs ea)
        {
            var properties = ea.BasicProperties;
            if (properties.ContentEncoding != null)
            {
                string messageBody;

                switch (properties.ContentEncoding.ToLowerInvariant())
                {
                    case "utf8":
                    case "utf-8":
                        messageBody = Encoding.UTF8.GetString(ea.Body);
                        break;
                    case "utf16":
                    case "utf-16":
                    case "unicode":
                        messageBody = Encoding.Unicode.GetString(ea.Body);
                        break;
                    default:
                        throw new ArgumentException("invalid content encoding: \"" + properties.ContentEncoding + "\"");
                }

                Console.WriteLine("Exchange:   {0}", ea.Exchange);
                Console.WriteLine("RoutingKey: {0}", ea.RoutingKey);

                if (properties.IsMessageIdPresent())
                    Console.WriteLine("Message-Id: {0}", properties.MessageId);
                if (properties.IsTimestampPresent())
                    Console.WriteLine("Timestamp:  {0}", properties.Timestamp);
                if (properties.IsCorrelationIdPresent())
                    Console.WriteLine("Correlation-Id: {0}", properties.CorrelationId);
                if (properties.IsTypePresent())
                    Console.WriteLine("Message-Type:   {0}", properties.Type);
                if (properties.IsReplyToPresent())
                    Console.WriteLine("ReplyTo:        {0}", properties.ReplyTo);

                Console.WriteLine();
                Console.WriteLine("========================= Message Body =========================");

                if (properties.ContentType.ToLowerInvariant() == "application/json")
                {
                    try
                    {
                        messageBody = JsonConvert.DeserializeObject<JObject>(messageBody).ToString();
                        Console.WriteLine(messageBody);
                    }
                    catch (JsonReaderException)
                    {
                        Console.WriteLine(messageBody);
                    }
                }
                else
                {
                    Console.WriteLine(messageBody);
                }

                Console.WriteLine("================================================================");
                Console.WriteLine();
            }
            else
            {
            }
        }

        /// <summary>
		/// Application entry point
		/// </summary>
		/// <param name="args"></param>
		public static void Main(string[] args)
		{
			try {
				_options = OptionParser.Parse<AppOptions>();

                // if there is no registered pattern, add a catch-all pattern
			    if (_options.Patterns.Count == 0)
			    {
			        _options.Patterns.Add("#");
			    }

			    var random = new Random();
			    var randomQueue = "wirerabbit-" + random.Next(0, 0x7fffffff).ToString("X").ToLowerInvariant();

			    Console.WriteLine("WireRabbit creating queue: {0}", randomQueue);

			    var factory = new ConnectionFactory() { Uri = _options.BrokerUri };
			    using (var connection = factory.CreateConnection())
			    {
			        using (var channel = connection.CreateModel())
			        {
			            channel.ExchangeDeclarePassive(_options.Exchange);
                        // declare a queue that will be used to consume messages
			            channel.QueueDeclare(randomQueue, false, false, true, new Dictionary<string, object>());
                        // bind the routing keys (patterns)
			            foreach (var pattern in _options.Patterns)
			            {
			                channel.QueueBind(randomQueue, _options.Exchange, pattern);
			                Console.WriteLine("WireRabbit binding to {0}", pattern);
                        }
                        // create a consumer
			            var consumer = new QueueingBasicConsumer(channel);
                        // consume the content
			            channel.BasicConsume(randomQueue, false, consumer);

			            Console.WriteLine("WireRabbit is listening ...");

			            while (true)
			            {
			                HandleMessage((BasicDeliverEventArgs) consumer.Queue.Dequeue());
			            }
			        }
			    }

			} catch( OptionParsingException e ) {
				Console.Error.WriteLine(e.Message);
				AppOptions.Usage();
			}
		}
	}
}
