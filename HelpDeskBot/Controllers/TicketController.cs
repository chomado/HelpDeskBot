using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using HelpDeskBot.Util;

namespace HelpDeskBot.Controllers
{
    public class TicketsController : ApiController
    {
        private static int nextTicketId = 1;
        private static Dictionary<int, Ticket> tickets = new Dictionary<int, Ticket>();

        [HttpPost]
        public IHttpActionResult Post(Ticket ticket)
        {
            int ticketId;

            Console.WriteLine("Ticket accepted: category:" + ticket.Category + " severity:" + ticket.Severity + " description:" + ticket.Description);

            lock (tickets)
            {
                ticketId = nextTicketId++;
                TicketsController.tickets.Add(key: ticketId, value: ticket);
            }
            return this.Ok(content: ticketId.ToString());
        }
    }
}
