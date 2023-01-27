using System.Diagnostics;
using System.Globalization;

namespace ChessAnalyzer.ChessLib;

/// <summary>
/// Class reprsenting time control settings of a game
/// </summary>
public class TimeControl
{
    /// <summary>
    /// List of Time Controls
    /// </summary>
    public List<Entry> Controls = new();

    /// <summary>
    /// Creates a time control object
    /// </summary>
    /// <param name="tc">Time Control represented as PGN TimeControl Tag-Value</param>
    public TimeControl(string tc = "")
    {
        if (tc == "") return;
        //9.6.1: Tag: TimeControl

        //This uses a list of one or more time control fields.Each field contains a
        //descriptor for each time control period; if more than one descriptor is present
        //then they are separated by the colon character(":").The descriptors appear
        //in the order in which they are used in the game.  The last field appearing is
        //considered to be implicitly repeated for further control periods as needed.

        //There are six kinds of TimeControl fields.

        //The first kind is a single question mark("?") which means that the time
        //control mode is unknown.When used, it is usually the only descriptor present.

        //The second kind is a single hyphen("-") which means that there was no time
        //control mode in use.When used, it is usually the only descriptor present.

        //The third Time control field kind is formed as two positive integers separated
        //by a solidus("/") character.The first integer is the number of moves in the
        //period and the second is the number of seconds in the period.Thus, a time
        //control period of 40 moves in 2 1 / 2 hours would be represented as "40/9000".

        //The fourth TimeControl field kind is used for a "sudden death" control period.
        //It should only be used for the last descriptor in a TimeControl tag value.It
        //is sometimes the only descriptor present.The format consists of a single
        //integer that gives the number of seconds in the period.Thus, a blitz game
        //would be represented with a TimeControl tag value of "300".

        //The fifth TimeControl field kind is used for an "incremental" control period.
        //It should only be used for the last descriptor in a TimeControl tag value and
        //is usually the only descriptor in the value.The format consists of two
        //positive integers separated by a plus sign("+") character.The first integer
        //gives the minimum number of seconds allocated for the period and the second
        //integer gives the number of extra seconds added after each move is made.So,
        //an incremental time control of 90 minutes plus one extra minute per move would
        //be given by "4500+60" in the TimeControl tag value.

        //The sixth TimeControl field kind is used for a "sandclock" or "hourglass"
        //control period.It should only be used for the last descriptor in a
        //TimeControl tag value and is usually the only descriptor in the value.The
        //format consists of an asterisk("*") immediately followed by a positive
        //integer.The integer gives the total number of seconds in the sandclock
        //period.The time control is implemented as if a sandclock were set at the
        //start of the period with an equal amount of sand in each of the two chambers
        //and the players invert the sandclock after each move with a time forfeit
        //indicated by an empty upper chamber.  Electronic implementation of a physical
        //sandclock may be used.An example sandclock specification for a common three
        //minute egg timer sandclock would have a tag value of "*180".

        try
        {
            string[] token = tc.Split(':');
            foreach (var t in token)
            {
                int time;
                int to = int.MaxValue;
                double inc = 0;
                int from = Controls.Count == 0 ? 1 : Controls.Last().To + 1;
                int indx1 = t.IndexOf("/");
                int indx2 = t.IndexOf("+");
                if (indx1 < 0 && indx2 < 0)
                {
                    Debug.Assert(Controls.Count == 0 || Controls.Last().To < int.MaxValue);
                    if (int.TryParse(t, out time))
                    {
                        Controls.Add(new Entry(from, int.MaxValue, TimeSpan.FromSeconds(time), TimeSpan.Zero));
                        continue;
                    }
                }
                else
                {
                    if (indx1 > 0) to = int.Parse(t.Substring(0, indx1)) + from - 1;
                    if (indx2 > 0) inc = double.Parse(t[(indx2 + 1)..], CultureInfo.InvariantCulture);
                    string t1 = indx2 > 0 ? t.Substring(0, indx2) : t;
                    if (indx1 > 0) t1 = t1[(indx1 + 1)..];
                    time = int.Parse(t1);
                    Controls.Add(new Entry(from, to, TimeSpan.FromSeconds(time), TimeSpan.FromSeconds(inc)));
                }
            }
        }
        catch (Exception)
        {
            return;
        }
    }
    /// <summary>
    /// Calculates the total available think time for all moves so far
    /// </summary>
    /// <param name="movenumber">Move number for which the total available time shall be calculated</param>
    public TimeSpan TotalAvailableTime(int movenumber)
    {
        TimeSpan total = TimeSpan.Zero;
        foreach (var entry in Controls)
        {
            if (movenumber > entry.To)
            {
                total += entry.Time + (entry.To - entry.From + 1) * entry.Increment;
            }
            else
            {
                total += entry.Time + (movenumber - entry.From + 1) * entry.Increment;
                break;
            }
        }
        if (movenumber > Controls.Last().To)
        {
            //special logic for repeating last increment
            int from = Controls.Last().To + 1;
            while (true)
            {
                int to = from + Controls.Last().To - Controls.Last().From;
                if (movenumber > to)
                {
                    total += Controls.Last().Time + (to - from + 1) * Controls.Last().Increment;
                }
                else
                {
                    total += Controls.Last().Time + (movenumber - from + 1) * Controls.Last().Increment;
                    break;
                }
                from = to + 1;
            }
        }
        return total;
    }
    /// <summary>
    /// Enhances Think Times, if only Clock values are available
    /// </summary>
    /// <param name="game">Game which will be enhanced</param>
    public void AddThinkTimes(UciGame game)
    {
        string[] ftoken = game.StartPosition.Split(' ');
        Side side = ftoken[1] == "w" ? Side.White : Side.Black;
        int movenumber = int.Parse(ftoken[5]);
        if (side == Side.White && movenumber == 1)
        {
            game.Moves[0].UsedThinkTime = TotalAvailableTime(1) - game.Moves[0].Clock;
            game.Moves[1].UsedThinkTime = TotalAvailableTime(1) - game.Moves[1].Clock;
        }
        for (int i = 2; i < game.Moves.Count; ++i)
        {
            game.Moves[i].UsedThinkTime = (game.Moves[i - 2].Clock - game.Moves[i].Clock) - (TotalAvailableTime(movenumber) - TotalAvailableTime(movenumber - 1));
            side = (Side)((int)side ^ 1);
            if (side == Side.White) ++movenumber;
        }
    }

    /// <summary>
    /// Single TimeControl 
    /// </summary>
    public class Entry
    {
        public Entry(int from, int to, TimeSpan time, TimeSpan increment)
        {
            From = from;
            To = to;
            Time = time;
            Increment = increment;
        }
        /// <summary>
        /// Validity of Time Control: Move number from which the time control is valid
        /// </summary>
        public int From { set; get; } = 1;
        /// <summary>
        /// Validity of Time Control: Last Move number for which the time control is valid
        /// </summary>
        public int To { set; get; } = int.MaxValue;
        /// <summary>
        /// Available Time 
        /// </summary>
        public TimeSpan Time { set; get; }
        /// <summary>
        /// Increment added for each move within this time interval
        /// </summary>
        public TimeSpan Increment { set; get; } = TimeSpan.Zero;
    }
}