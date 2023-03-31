using System.Threading.Channels;

public struct Token
{
    public string data;
    public int recipient;
    public int ttl;
}

public class TokenRing
{
    public int N { get; set; }
    public static int new_N = 0;
    public TokenRing()
    {
        N = new_N++;
    }
    public static async Task GetTokenRingTask(int n, Token t)
    {
        List<Channel<Token>> CreateChanel = new List<Channel<Token>>
        {
            Channel.CreateBounded<Token>(new BoundedChannelOptions(1))
        };
        await CreateChanel[0].Writer.WriteAsync(t);

        List<Task> tasks = new List<Task>();
        for (int i = 1; i < n; i++)
        {
            CreateChanel.Add(Channel.CreateBounded<Token>(new BoundedChannelOptions(1)));
            int number = i;
            tasks.Add(ViewProgress(number, CreateChanel[number], CreateChanel[number - 1]));
        }
        await Task.WhenAll(tasks);
    }
    public static async Task ViewProgress(int tasknumber, Channel<Token> NextStep, Channel<Token, Token> PreviousStep)
    {
        var startTime = DateTime.Now;
        await PreviousStep.Reader.WaitToReadAsync();
        var token = await PreviousStep.Reader.ReadAsync();
        if (token.recipient == tasknumber)
        {
            Console.WriteLine("Номер потока: {0},  сообщение от узла: {1} ", tasknumber, token.data);
        }
        else
        {
            Console.WriteLine("Номер потока: {0}, сообщение не достигло адресата", tasknumber);
        }
        await NextStep.Writer.WriteAsync(token);
        var endTime = DateTime.Now;
        if ((endTime - startTime).TotalSeconds > token.ttl)
            throw new Exception("Время жизни иссякло.");
    }
}

public class MainProg
{
    static async Task Main(string[] args)
    {
        int entered_N;
        Console.WriteLine("Введите количество потоков");
        entered_N = Convert.ToInt32(Console.ReadLine());
        
        Token tokendefault = new Token
        {
            data = "Я - сообщение",
            recipient = entered_N - 1,
            ttl = 1,
        };
        await TokenRing.GetTokenRingTask(entered_N, tokendefault);
    }
}