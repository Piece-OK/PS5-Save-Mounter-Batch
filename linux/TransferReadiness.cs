namespace PS4Saves.Linux;

public static class TransferReadiness
{
    public static void Test(SaveFtp ftp, MounterClient mounter)
    {
        try { ftp.TestConnection(); }
        catch (Exception ftpError)
        {
            // Use the existing session: TcpClient.Connected alone is not a health check.
            bool mounterResponding;
            try { mounter.Ping(); mounterResponding = true; }
            catch { mounterResponding = false; }
            string message = mounterResponding
                ? "FTP server unavailable (stopped or unreachable); savemounter.elf is still responding. Restart FTP and reconnect before retrying."
                : "FTP connection failed; savemounter.elf did not respond either. Check the PS5 connection and services, then reconnect.";
            throw new IOException(message + " FTP detail: " + ftpError.Message, ftpError);
        }
    }
}
