using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Timeout;
using RestSharp;

namespace RequestService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        [HttpGet("MakeRequest1")]
        public async Task<ActionResult> MakeRequest1()
        {
            // Retry policy
            // આ પોલિસી રીક્વેસ્ટનો પ્રત્યુત્તર સફળ ન હોય તો તેને હેન્ડલ કરે છે.
            // આ પોલિસી પાંચ વખત રીક્વેસ્ટને ફરી મોકલે છે,
            // દરેક વખત એક સેકન્ડની વિચકાર રાખીને.
            // દરેક રીટ્રાયમાં કોન્સોલમાં રીટ્રાય નંબર છાપવામાં આવે છે.
            var retryPolicy = Policy.HandleResult<RestResponse>(r => !r.IsSuccessful)
                .WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(1), (_, _, retryNumber, _) => Console.WriteLine($"Retrying: {retryNumber}"));

            // Circuit Breaker policy
            //આ પોલિસી પણ રીક્વેસ્ટનો પ્રત્યુત્તર સફળ ન હોય તો તેને હેન્ડલ કરે છે.
            //આ પોલિસી બે ફેલ રીક્વેસ્ટ પછી સર્કિટને બ્રેક કરે છે,
            //અને દસ સેકન્ડ સુધી કોઈ રીક્વેસ્ટને મોકલવાની પરવાનગી નથી.
            //સર્કિટ બ્રેક થાય તેવી અને રીસેટ થાય તેવી સ્થિતિમાં કોન્સોલમાં સંદેશ છાપવામાં આવે છે.
            var breakerPolicy = Policy.HandleResult<RestResponse>(r => !r.IsSuccessful)
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10), (_, _, _) => Console.WriteLine("Breaker Hit"), _ => { });

            // Timeout policy
            // આ પોલિસી રીક્વેસ્ટને પાંચ સેકન્ડમાં પૂરી થવાની શરત મુકે છે.
            // જો રીક્વેસ્ટ પાંચ સેકન્ડમાં પૂરી ન થાય તો તેને કેન્સલ કરી દેવામાં આવે છે
            var timeoutPolicy = Policy.TimeoutAsync<RestResponse>(5, TimeoutStrategy.Pessimistic); // setup the timeout limit to be 1 sec


            // Fallback policy
            //આ પોલિસી રીક્વેસ્ટનો પ્રત્યુત્તર સફળ ન હોય તેવી સ્થિતિમાં કામ કરે છે.
            //જો રીક્વેસ્ટ નિષ્ફળ થાય તો તે ફોલબેક રિસ્પોન્સ પરત આપે છે
            var fallbackPolicy = Policy<RestResponse>.HandleResult(r => !r.IsSuccessful)
     .FallbackAsync((ct) => Task.FromResult(new RestResponse { Content = "Fallback response" }));

            // Bulkhead policy
            // આ પોલિસી એક સમયે કેટલાં રીક્વેસ્ટ મોકલવાની મર્યાદા નિર્ધારિત કરે છે.
            // આ પોલિસી એક સમયે ફક્ત એક રીક્વેસ્ટ મોકલે છે
            var bulkheadPolicy = Policy.BulkheadAsync<RestResponse>(1, 1);

            // Wrap all policies
            // આ લાઇન બધી પોલિસીઓને એક સાથે વાપરે છે.
            // આ પોલિસી રીક્વેસ્ટને મોકલવા પહેલાં બધી પોલિસીઓને ચકાસે છે.
            var policy = Policy.WrapAsync(retryPolicy, breakerPolicy, timeoutPolicy, bulkheadPolicy, fallbackPolicy);
            var client = new RestClient("https://localhost:7156/api");

            var request = new RestRequest("/Response/100");

            var response = await policy.ExecuteAsync(async () => await client.ExecuteAsync(request));

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("--> ResponseService returned a Success");
                return Ok("--> ResponseService returned a Success");
            }
            else
            {
                Console.WriteLine("--> ResponseService returned a FAILURE");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }
        [HttpGet("MakeRequest")]
        public async Task<ActionResult> MakeRequest()
        {
            // Retry policy
            var retryPolicy = Policy.HandleResult<RestResponse<string>>(r => !r.IsSuccessful)
                .WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(1), (_, _, retryNumber, _) => Console.WriteLine($"Retrying: {retryNumber}"));

            // Circuit Breaker policy
            var breakerPolicy = Policy.HandleResult<RestResponse<string>>(r => !r.IsSuccessful)
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10), (_, _, _) => Console.WriteLine("Breaker Hit"), _ => { });

            // Timeout policy
            var timeoutPolicy = Policy.TimeoutAsync<RestResponse<string>>(5, TimeoutStrategy.Pessimistic); // setup the timeout limit to be 1 sec


            // Fallback policy

            var fallbackPolicy = Policy.HandleResult<RestResponse>(r => !r.IsSuccessful)
    .FallbackAsync(new RestResponse { Content = "Fallback response" });

            //        var fallbackPolicy = Policy<RestResponse<string>>.HandleResult(r => !r.IsSuccessful)
            //.FallbackAsync(new RestResponse<string> { Content = "Fallback response" });
            // Bulkhead policy
            var bulkheadPolicy = Policy.BulkheadAsync<RestResponse<string>>(1, 1);

            // Wrap all policies
            var policy = Policy.WrapAsync(retryPolicy, breakerPolicy, timeoutPolicy, bulkheadPolicy);

            var client = new RestClient("https://localhost:7193/api");

            var request = new RestRequest("/Response/100");

            var response = await policy.ExecuteAsync(async () => await client.ExecuteAsync<string>(request));

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("--> ResponseService returned a Success");
                return Ok("--> ResponseService returned a Success");
            }
            else
            {
                Console.WriteLine("--> ResponseService returned a FAILURE");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
