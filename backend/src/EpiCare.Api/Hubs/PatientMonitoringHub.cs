using Microsoft.AspNetCore.SignalR;

namespace EpiCare.Api.Hubs;

public sealed class PatientMonitoringHub : Hub
{
    public Task JoinPatient(string patientId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, patientId);
    }

    public Task LeavePatient(string patientId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, patientId);
    }
}
