﻿#region Licence
/* The MIT License (MIT)
Copyright © 2015 Yiannis Triantafyllopoulos <yiannis.triantafyllopoulos@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using paramore.brighter.commandprocessor.Logging;

namespace paramore.brighter.commandprocessor.messaginggateway.azureservicebus
{
    public class AzureServiceBusMessageProducer : MessageGateway, IAmAMessageProducerSupportingDelay
    {
        private readonly ILog logger;
        private readonly MessageSenderPool pool;

        public AzureServiceBusMessageProducer(ILog logger)
            : base(logger)
        {
            this.logger = logger;
            this.pool = new MessageSenderPool(logger, factory);
        }

        public void Send(Message message)
        {
            SendWithDelay(message);
        }

        public void SendWithDelay(Message message, int delayMilliseconds = 0)
        {
            logger.DebugFormat("AzureServiceBusMessageProducer: Publishing message to topic {0}", message.Header.Topic);
            var messageSender = pool.GetMessageSender(message.Header.Topic);
            EnsureTopicExists(message.Header.Topic);
            messageSender.Send(new BrokeredMessage(message.Body.Value)
            {
                ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMilliseconds(delayMilliseconds)
            });
            logger.DebugFormat("AzureServiceBusMessageProducer: Published message with id {0} to topic '{1}' with a delay of {2}ms and content: {3}", message.Id, message.Header.Topic, delayMilliseconds, JsonConvert.SerializeObject(message));
        }

        public bool DelaySupported
        {
            get { return true; }
        }

        ~AzureServiceBusMessageProducer()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pool != null)
                {
                    pool.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}