using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasBot
{
    public class CmdModule : BaseCommandModule
    {
        [Command("assignments")]
        public async Task AssignmentsCommand(CommandContext ctx, int hours)
        {
            await ctx.RespondAsync($"Assignments due within {hours} hours: \n{Bot.Assignments(hours)}");
        }

        [Command("assignments")]
        public async Task AssignmentsCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"Assignments due within 48 hours: \n{Bot.Assignments(48)}");
        }

        [Command("courses")]
        public async Task CoursesCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"Courses currently enrolled: \n{Bot.Courses()}");
        }
    }
}
