﻿#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

using paramore.brighter.commandprocessor;
using paramore.brighter.commandprocessor.logging;
using paramore.brighter.commandprocessor.logging.Attributes;
using paramore.brighter.commandprocessor.Logging;
using paramore.brighter.commandprocessor.policy.Attributes;
using Tasks.Adapters.DataAccess;
using Tasks.Model;
using Tasks.Ports.Commands;
using Tasks.Ports.Events;

namespace Tasks.Ports.Handlers
{
    public class AddTaskCommandHandler : RequestHandler<AddTaskCommand>
    {
        private readonly ITasksDAO _tasksDAO;
        private readonly IAmACommandProcessor _commandProcessor;

        public AddTaskCommandHandler(ITasksDAO tasksDAO, IAmACommandProcessor commandProcessor) 
            : this(tasksDAO, commandProcessor, LogProvider.For<AddTaskCommandHandler>())
        {}


        public AddTaskCommandHandler(ITasksDAO tasksDAO, IAmACommandProcessor commandProcessor, ILog logger)
            : base(logger)
        {
            _tasksDAO = tasksDAO;
            _commandProcessor = commandProcessor;
        }

        [RequestLogging(step: 1, timing: HandlerTiming.Before)]
        [Validation(step: 2, timing: HandlerTiming.Before)]
        [UsePolicy(CommandProcessor.RETRYPOLICY, step: 3)]
        public override AddTaskCommand Handle(AddTaskCommand addTaskCommand)
        {
            using (var scope = _tasksDAO.BeginTransaction())
            {
                var inserted = _tasksDAO.Add(
                    new Task(
                        taskName: addTaskCommand.TaskName,
                        taskDecription: addTaskCommand.TaskDescription,
                        dueDate: addTaskCommand.TaskDueDate
                        )
                    );

                scope.Commit();

                addTaskCommand.TaskId = inserted.Id;
            }

            _commandProcessor.Post(new TaskAddedEvent(addTaskCommand.Id, addTaskCommand.TaskId, addTaskCommand.TaskName, addTaskCommand.TaskDescription, addTaskCommand.TaskDueDate));

            return base.Handle(addTaskCommand);
        }
    }
}