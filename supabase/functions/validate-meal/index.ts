import { serve } from "https://deno.land/std@0.177.0/http/server.ts"
import { createClient } from "https://esm.sh/@supabase/supabase-js@2"

const corsHeaders = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

serve(async (req) => {
    if (req.method === 'OPTIONS') {
        return new Response('ok', { headers: corsHeaders })
    }

    try {
        const { imageBase64, userId, challengeId } = await req.json()
        const GEMINI_API_KEY = Deno.env.get('GEMINI_API_KEY')

        // 1. Prepare the request for Gemini 1.5 Flash
        const prompt = "Look at this image. Is it a healthy meal (like a salad, fruit, or protein with veggies)? Answer in JSON format: { \"isHealthy\": true, \"reason\": \"Because it contains...\" }";

        const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=${GEMINI_API_KEY}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                contents: [{
                    parts: [
                        { text: prompt },
                        { inline_data: { mime_type: "image/jpeg", data: imageBase64 } }
                    ]
                }],
                generationConfig: {
                    response_mime_type: "application/json",
                }
            })
        })

        const data = await response.json()
        // Extract the text from Gemini's complex response structure
        const aiText = data.candidates[0].content.parts[0].text
        const result = JSON.parse(aiText)

        // 2. If AI says it's healthy, update Supabase
        if (result.isHealthy) {
            const supabase = createClient(
                Deno.env.get('SUPABASE_URL')!,
                Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
            )

            await supabase.from('user_challenge_completion').insert({
                user_id: userId,
                challenge_id: challengeId,
                completed_date: new Date().toISOString().split('T')[0]
            })
        }

        return new Response(JSON.stringify(result), {
            headers: { ...corsHeaders, 'Content-Type': 'application/json' },
            status: 200,
        })

    } catch (error) {
        return new Response(JSON.stringify({ error: error.message }), {
            headers: { ...corsHeaders, 'Content-Type': 'application/json' },
            status: 400,
        })
    }
})