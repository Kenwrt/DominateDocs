using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsNotify.Models;
using DominateDocsNotify.State;

namespace DominateDocsSite.ViewModels;

public partial class ContactUsViewModel : ObservableObject
{
    [ObservableProperty]
    private EmailMsg editingMailMsg = null;

    [ObservableProperty]
    private EmailMsg selectedMailMsg = null;

    public INotifyState? NotifyState { get; set; }

    public ContactUsViewModel(INotifyState? NotifyState)
    {
        this.NotifyState = NotifyState;
    }

    [RelayCommand]
    private async Task InitializePageAsync()
    {
        if (EditingMailMsg is null)
        {
            EditingMailMsg = new();
        }
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        //if (EditingAgreement is null)
        //{
        //    EditingAgreement = GetNewRecord();
        //}

        //if (EditingAgreement.DownPaymentPercentage > 0)
        //{
        //    EditingAgreement.DownPaymentAmmount = EstimatedDownPayment;

        //    EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);

        //    EditingAgreement.DownPaymentAmmount = EstimatedDwnPaymentAmount;
        //}

        //if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
        //{
        //    GetLoanMaturityDate(EditingAgreement.FixedInterestProperties.TermInMonths);
        //}
        //else
        //{
        //    GetLoanMaturityDate(EditingAgreement.VariableInterestProperties.TermInMonths);
        //}
    }

    [RelayCommand]
    private void UpsertMailMsg()
    {
        try
        {
            //dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private void EditMailMsg()
    {
        //dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);

        //AgreementList.Clear();

        //dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().ToList().ForEach(r => AgreementList.Add(r));

        //SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void DeleteMailMsg()
    {
        //if (SelectedAgreement != null)
        //{
        //    AgreementList.Remove(SelectedAgreement);

        //    dbApp.DeleteRecord<DominateDocsData.Models.LoanAgreement>(SelectedAgreement);

        //    SelectedAgreement = null;
        //    EditingAgreement = GetNewRecord();
        //}
    }

    [RelayCommand]
    private void SelectMailMsg(EmailMsg r)
    {
        //SelectedAgreement = EditingAgreement;

        //if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
        //{
        //    GetLoanMaturityDate(EditingAgreement.FixedInterestProperties.TermInMonths);
        //}
        //else
        //{
        //    GetLoanMaturityDate(EditingAgreement.VariableInterestProperties.TermInMonths);
        //}
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedMailMsg != null)
        {
            SelectedMailMsg = null;

            EditingMailMsg = new();
        }
    }

    [RelayCommand]
    private async Task AddMailMsg()
    {
        //AgreementList.Add(EditingAgreement);

        //dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    [RelayCommand]
    private void SendMail()
    {
        try
        {
            EmailMsg mailMSG = new()
            {
                To = EditingMailMsg.ReplyTo,
                Subject = "Contact Us Message",
                PostMarkTemplateId = (int)DominateDocsNotify.Enums.EmailEnums.Templates.ContactUs,
                TemplateModel = new
                {
                    login_url = "https://DominateDocs.law/account/login",
                    username = EditingMailMsg.Name ?? string.Empty,
                    product_name = "DominateDocs",
                    support_email = "https://DominateDocs.law/support",
                    help_url = "https://DominateDocs.law/help",
                    name = EditingMailMsg.Name,
                    phone = EditingMailMsg.Phone,
                    email = EditingMailMsg.ReplyTo,
                    message = EditingMailMsg.MessageBody
                }
            };

            NotifyState.EmailMsgProcessingQueue.Enqueue(mailMSG);

            mailMSG = new()
            {
                To = "ContactUs@DominateDocs.law",
                Subject = "User Contact Message",
                MessageBody = $"From: {EditingMailMsg.Name} Phone: {EditingMailMsg.Phone} Message: {EditingMailMsg.MessageBody}",
                PostMarkTemplateId = (int)DominateDocsNotify.Enums.EmailEnums.Templates.UserContact,
                TemplateModel = new
                {
                    login_url = "https://DominateDocs.law/account/login",
                    username = EditingMailMsg.Name ?? string.Empty,
                    product_name = "DominateDocs",
                    support_email = "https://DominateDocs.law/support",
                    help_url = "https://DominateDocs.law/help",
                    name = EditingMailMsg.Name,
                    email = EditingMailMsg.ReplyTo,
                    message = EditingMailMsg.MessageBody
                }
            };

            NotifyState.EmailMsgProcessingQueue.Enqueue(mailMSG);
        }
        catch (System.Exception ex)
        {
            throw;
        }
    }
}