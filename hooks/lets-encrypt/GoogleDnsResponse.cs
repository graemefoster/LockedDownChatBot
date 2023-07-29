namespace LetsEncrypt;

public class GoogleDnsResponse
{
    public int Status { get; set; }
    public Answer[] Answer { get; set; }
}