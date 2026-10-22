interface SentinelTokenSet {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
}

interface Window {
    sentinelAuth?: {
        startLogin: () => Promise<SentinelTokenSet>;
    };
}
