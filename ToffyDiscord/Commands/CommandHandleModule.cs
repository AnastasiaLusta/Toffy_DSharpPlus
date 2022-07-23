﻿// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2022 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;

namespace ToffyDiscord.Commands;

public static class CommandHandleModule
{
    public static Task Handle(DiscordClient client, MessageCreateEventArgs e)
    {
        var nextCommand = client.GetCommandsNext();
        var msg = e.Message;

        var cmdStart = msg.GetStringPrefixLength("!");
        if (cmdStart == -1) return Task.CompletedTask;

        var prefix = msg.Content[..cmdStart];
        var cmdString = msg.Content[cmdStart..];

        var command = nextCommand.FindCommand(cmdString, out var args);
        if (command == null)
            return Task.CompletedTask;

        var ctx = nextCommand.CreateContext(msg, prefix, command, args);
        Task.Run(async () => await nextCommand.ExecuteCommandAsync(ctx));

        return Task.CompletedTask;
    }
}