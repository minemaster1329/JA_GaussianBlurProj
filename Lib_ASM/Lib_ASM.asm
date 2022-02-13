.code

;input variables
;rcx - pointer to pixels array
;rdx - pointer to weights array
;xmm2 - sumOfWeights
;r9 - diameter

;registers used in function
;r9 - diameter
;xmm0 - output
;r10 - pointer to pixels array
;r11 - pointer to weights array
;xmm2 - sumOfweights
;rcx - counter
CalculatePixelAsm proc
                        imul r9, r9
                        xorps xmm0, xmm0
                        mov r10, rcx
                        mov r11, rdx
                        xor r8, r8 
                        xor rcx, rcx
loop_start:             movss xmm1, dword ptr [r10 + rcx*4]
                        mulss xmm1, dword ptr [r11 + rcx*4]
                        addss xmm0, xmm1
                        inc rcx
                        cmp rcx, r9
                        jl loop_start
                        divss xmm0, xmm2
                        ret
CalculatePixelAsm    endp
CalculatePixelAsm1 proc
                        imul r9, r9                                             ;raising diameter variable to 2-nd power
                        xorps xmm0, xmm0                                        ;zeroing xmm register
                        mov r10, rcx                                            ;moving pointer to pixels array to r10
                        mov r11, rdx                                            ;moving pointer to weights array to r11
                        xor rcx, rcx                                            ;zeroing rcx
                        mov rdx, r9                                             ;moving diameter value to rdx
                        shr rdx, 2                                              ;dividing diameter by 4
loop_start1:            xorps xmm1, xmm1                                        ;zeroing xmm1
                        xorps xmm3, xmm3                                        ;zeroing xmm3
                        xorps xmm4, xmm4                                        ;zeroing xmm4

                        mov r8, rcx                                             ;moving rcx to r8
                        shl rcx, 4                                              ;shifting r8 by 4

                        vinsertps xmm3, xmm3,dword ptr[r10+rcx], 0              ;moving c-th element of pixels array to xmm3[0]
                        vinsertps xmm3, xmm3,dword ptr[r10+rcx + 4], 16         ;moving (c+1)-th element of pixels array to xmm3[1]
                        vinsertps xmm3, xmm3,dword ptr[r10+rcx + 8], 32         ;moving (c+2)-th element of pixels array to xmm3[2]
                        vinsertps xmm3, xmm3,dword ptr[r10+rcx + 12], 48        ;moving (c+3)-th element of pixels array to xmm3[3]

                        vinsertps xmm1, xmm1,dword ptr[r11+rcx], 8              ;moving c-th element of weights array to xmm3[0]
                        vinsertps xmm1, xmm1,dword ptr[r11+rcx + 4], 16         ;moving (c+1)-th element of weights array to xmm3[1]
                        vinsertps xmm1, xmm1,dword ptr[r11+rcx + 8], 32         ;moving (c+2)-th element of weights array to xmm3[2]
                        vinsertps xmm1, xmm1,dword ptr[r11+rcx + 12], 48        ;moving (c+3)-th element of weights array to xmm3[3]

                        mov rcx, r8                                             ;restoring original value of rcx

                        vmulps xmm3, xmm3,xmm1                                  ;multiplying xmm3 vector by xmm1 and moving result to xmm3

                        EXTRACTPS r8d, xmm3, 0                                  ;extracting xmm3[0] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addps xmm0, xmm4                                        ;adding xmm3[0] to output variable 
                        
                        EXTRACTPS r8d, xmm3, 1                                  ;extracting xmm3[1] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addps xmm0, xmm4                                        ;adding xmm3[1] to output variable

                        EXTRACTPS r8d, xmm3, 2                                  ;extracting xmm3[2] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addps xmm0, xmm4                                        ;adding xmm3[2] to output variable

                        EXTRACTPS r8d, xmm3, 3                                  ;extracting xmm[3] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addps xmm0, xmm4                                        ;adding xmm3[3] to output variable

                        inc rcx                                                 ;increment rcx
                        cmp rcx, rdx                                            ;compare counter with diameter / 4
                        jl loop_start1                                          ;if counter is lower than diameter / 4, move to loop_start
                        shl rcx, 2                                              ;multiplying counter by 4
                        cmp rcx, r9                                             ;comparing counter with squared diameter
                        je end_p                                                ;if counter is equal to squared diameter, jomping to end of program
                        mov rdx, r9                                             ;moving diameter value to rdx
loop_start2:            xorps xmm1, xmm1                                        ;zeroing xmm1
                        movss xmm1, dword ptr[r10+rcx*4]                        ;moving n-th element of pixels array to xmm1
                        mulss xmm1, dword ptr[r11+rcx*4]                        ;multiplying xmm1 by n-th element of weights array
                        addps xmm0, xmm1                                        ;adding value to output register
                        inc rcx                                                 ;incrementing counter
                        cmp rcx, rdx                                            ;comparing counter with squared diameter
                        jl loop_start2                                          ;if counter is lower than squared diameter, jumping to begin of loop 2
                        divss xmm0, xmm2                                        ;divide output by sum of weights
end_p:                  ret                                                     ;return from procedure
CalculatePixelAsm1 endp
end
end

