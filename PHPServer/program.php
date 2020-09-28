<?php
include "vk_api.php";
Program::receiveFromPuppeteer();
class Auth
{
    // Standalone app
    public const APP_KEY = "7599776";
    public const APP_SECURED_KEY = "Imux7zhZsb683RrpYzx7";
    public const APP_SERVICE_ACCESS_KEY = "929361c6929361c6929361c66d92e0976699293929361c6cdf0215a81843b7bbf4f6feb";
    // Group settings
    public const GROUP_TOKEN = 'ed25a9c53b9b5749ede6c690c50f166cdfbbd41f7de6adc6db141a423342c6c6fdbaefa18fab38a27716a';
    public const GROUP_CONFIRM_KEY = 'ccc5736f';

    public const VERSION_API = '5.81';
}
class Program
{
    static function receiveFromPuppeteer(){
        $newDzMsg = file_get_contents('php://input');
        $vk = new vk_api(Auth::GROUP_TOKEN, Auth::VERSION_API);
        $vk->sendOK();
        $fileLocation = getenv("DOCUMENT_ROOT") . "/peer_id.txt";
        $peer_id = file_get_contents($fileLocation);
        $vk->sendMessage($peer_id, $newDzMsg);
    }
}