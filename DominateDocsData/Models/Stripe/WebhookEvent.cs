using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class WebhookEvent
{
    public Guid Id { get; set; } = default!;

    public string Type { get; set; } = default!;

    public EventData Data { get; set; } = default!;
}
