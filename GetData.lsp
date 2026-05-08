;;; GetData.lsp - Extract selected AutoCAD objects and send to SiteTrack Bridge App
;;; For AutoCAD 2007+
;;;
;;; Load in AutoCAD with: (load "GetData.lsp")
;;; Run with: getData
;;;
;;; Extracts: Points, Lines, Curves (Arcs), and Text
;;; Sends via Named Pipe to WinForms app

(defun c:getData (/ ss i ent obj type points lines curves texts idx
                     point-data line-data curve-data text-data)
  "Extract selected objects and send to helper app"

  (princ "\n>>> SiteTrack Bridge - Getting Data from Selection...\n")

  ;; Get user selection
  (setq ss (ssget))
  (if (null ss)
    (progn
      (princ "No objects selected. Please select objects and run getData again.\n")
      (exit)
    )
  )

  ;; Initialize collections
  (setq points (list))
  (setq lines (list))
  (setq curves (list))
  (setq texts (list))
  (setq idx 0)

  ;; Process each selected object
  (princ (strcat "Processing " (itoa (sslength ss)) " objects...\n"))

  (repeat (sslength ss)
    (setq ent (ssname ss idx))
    (setq obj (entget ent))
    (setq type (cdr (assoc 0 obj)))

    ;; Extract based on entity type
    (cond
      ;; POINT entity
      ((= type "POINT")
        (setq point-data (extract-point ent obj))
        (if point-data
          (setq points (cons point-data points))
        )
      )

      ;; LINE entity
      ((= type "LINE")
        (setq line-data (extract-line ent obj))
        (if line-data
          (setq lines (cons line-data lines))
        )
      )

      ;; LWPOLYLINE entity (lightweight polyline)
      ((= type "LWPOLYLINE")
        (setq line-data (extract-lwpolyline ent obj))
        (if line-data
          (setq lines (cons line-data lines))
        )
      )

      ;; ARC entity
      ((= type "ARC")
        (setq curve-data (extract-arc ent obj))
        (if curve-data
          (setq curves (cons curve-data curves))
        )
      )

      ;; CIRCLE entity
      ((= type "CIRCLE")
        (setq curve-data (extract-circle ent obj))
        (if curve-data
          (setq curves (cons curve-data curves))
        )
      )

      ;; TEXT entity
      ((= type "TEXT")
        (setq text-data (extract-text ent obj))
        (if text-data
          (setq texts (cons text-data texts))
        )
      )

      ;; MTEXT entity
      ((= type "MTEXT")
        (setq text-data (extract-mtext ent obj))
        (if text-data
          (setq texts (cons text-data texts))
        )
      )
    )

    (setq idx (+ idx 1))
  )

  ;; Report results
  (princ (strcat "\n✓ Points: " (itoa (length points)) "\n"))
  (princ (strcat "✓ Lines: " (itoa (length lines)) "\n"))
  (princ (strcat "✓ Curves: " (itoa (length curves)) "\n"))
  (princ (strcat "✓ Texts: " (itoa (length texts)) "\n"))
  (princ (strcat "✓ Total: " (itoa (+ (length points) (length lines) (length curves) (length texts))) "\n"))

  ;; Send data to app via named pipe
  (send-to-pipe points lines curves texts)

  (princ "\n✓ Data sent to SiteTrack Bridge App!\n")
  (exit)
)

;;; Extract POINT
(defun extract-point (ent obj / pt layer name props)
  (setq pt (cdr (assoc 10 obj)))
  (setq layer (cdr (assoc 8 obj)))

  (list
    (list "type" "POINT")
    (list "E" (nth 0 pt))
    (list "N" (nth 1 pt))
    (list "layer" layer)
  )
)

;;; Extract LINE
(defun extract-line (ent obj / p1 p2 layer name props)
  (setq p1 (cdr (assoc 10 obj)))
  (setq p2 (cdr (assoc 11 obj)))
  (setq layer (cdr (assoc 8 obj)))

  (list
    (list "type" "LINE")
    (list "startE" (nth 0 p1))
    (list "startN" (nth 1 p1))
    (list "endE" (nth 0 p2))
    (list "endN" (nth 1 p2))
    (list "layer" layer)
  )
)

;;; Extract LWPOLYLINE
(defun extract-lwpolyline (ent obj / pts layer p1 p2 idx)
  (setq layer (cdr (assoc 8 obj)))
  (setq pts (get-lwpolyline-points ent))

  (if (>= (length pts) 2)
    (progn
      (setq p1 (car pts))
      (setq p2 (cadr pts))

      (list
        (list "type" "LWPOLYLINE")
        (list "startE" (nth 0 p1))
        (list "startN" (nth 1 p1))
        (list "endE" (nth 0 p2))
        (list "endN" (nth 1 p2))
        (list "vertexCount" (length pts))
        (list "layer" layer)
      )
    )
    nil
  )
)

;;; Get vertices from LWPOLYLINE
(defun get-lwpolyline-points (ent / obj pts pt i)
  (setq obj (entget ent))
  (setq pts (list))

  ;; Walk entire association list, collecting every code 10 (vertex)
  (foreach item obj
    (if (= (car item) 10)
      (setq pts (cons (cdr item) pts))
    )
  )

  ;; Return in original order
  (reverse pts)
)

;;; Extract ARC
(defun extract-arc (ent obj / center radius start-angle end-angle layer id)
  (setq center (cdr (assoc 10 obj)))
  (setq radius (cdr (assoc 40 obj)))
  (setq start-angle (cdr (assoc 50 obj)))
  (setq end-angle (cdr (assoc 51 obj)))
  (setq layer (cdr (assoc 8 obj)))
  (setq id (strcat "arc-" (itoa (rem (abs (sslength (ssget))) 10000))))

  (list
    (list "id" id)
    (list "name" id)
    (list "entityType" "ARC")
    (list "centerE" (nth 0 center))
    (list "centerN" (nth 1 center))
    (list "radius" radius)
    (list "startAngle" start-angle)
    (list "endAngle" end-angle)
    (list "layer" layer)
  )
)

;;; Extract CIRCLE
(defun extract-circle (ent obj / center radius layer id)
  (setq center (cdr (assoc 10 obj)))
  (setq radius (cdr (assoc 40 obj)))
  (setq layer (cdr (assoc 8 obj)))
  (setq id (strcat "circ-" (itoa (rem (abs (sslength (ssget))) 10000))))

  (list
    (list "id" id)
    (list "name" id)
    (list "entityType" "CIRCLE")
    (list "centerE" (nth 0 center))
    (list "centerN" (nth 1 center))
    (list "radius" radius)
    (list "layer" layer)
  )
)

;;; Extract TEXT
(defun extract-text (ent obj / content pt layer)
  (setq content (cdr (assoc 1 obj)))
  (setq pt (cdr (assoc 10 obj)))
  (setq layer (cdr (assoc 8 obj)))

  (list
    (list "type" "TEXT")
    (list "content" content)
    (list "E" (nth 0 pt))
    (list "N" (nth 1 pt))
    (list "layer" layer)
  )
)

;;; Remove MTEXT formatting codes (properly)
(defun remove-mtext-formatting (str)
  ;; Handle paragraph breaks: \P -> space
  (setq str (vl-string-subst " " "\\P" str))

  ;; Handle alignment codes: \A0, \A1, \A2, etc -> remove
  (setq str (vl-string-subst "" "\\A0" str))
  (setq str (vl-string-subst "" "\\A1" str))
  (setq str (vl-string-subst "" "\\A2" str))

  ;; Handle color codes: \C1, \C2, etc -> remove
  (setq str (vl-string-subst "" "\\C1" str))
  (setq str (vl-string-subst "" "\\C2" str))
  (setq str (vl-string-subst "" "\\C3" str))

  ;; Handle font codes: \F... -> remove (more complex, skip for now)

  ;; Handle Unicode sequences: \U+xxxx -> remove
  ;; This is harder, so just handle the most common pattern

  str
)

;;; Extract MTEXT
(defun extract-mtext (ent obj / content pt layer)
  (setq content (cdr (assoc 1 obj)))
  (setq pt (cdr (assoc 10 obj)))
  (setq layer (cdr (assoc 8 obj)))

  ;; Clean up MTEXT formatting codes
  (setq content (remove-mtext-formatting content))

  (list
    (list "type" "MTEXT")
    (list "content" content)
    (list "E" (nth 0 pt))
    (list "N" (nth 1 pt))
    (list "layer" layer)
  )
)

;;; Send data to WinForms app via file-based handshake
(defun send-to-pipe (points lines curves texts / json-str)
  (setq json-str (build-json points lines curves texts))

  ;; Write JSON to Documents folder - the WinForms app watches this file
  (if (> (strlen json-str) 0)
    (write-json-file json-str)
  )
)

;;; Build JSON string from extracted data
(defun build-json (points lines curves texts / json-str)
  (setq json-str "{")
  (setq json-str (strcat json-str "\"points\":["))
  (setq json-str (strcat json-str (build-json-array points)))
  (setq json-str (strcat json-str "],"))

  (setq json-str (strcat json-str "\"lines\":["))
  (setq json-str (strcat json-str (build-json-array lines)))
  (setq json-str (strcat json-str "],"))

  (setq json-str (strcat json-str "\"curves\":["))
  (setq json-str (strcat json-str (build-json-array curves)))
  (setq json-str (strcat json-str "],"))

  (setq json-str (strcat json-str "\"texts\":["))
  (setq json-str (strcat json-str (build-json-array texts)))
  (setq json-str (strcat json-str "]"))

  (setq json-str (strcat json-str "}"))
  json-str
)

;;; Build JSON array from list
(defun build-json-array (items / result idx)
  (setq result "")
  (setq idx 0)

  (repeat (length items)
    (if (> idx 0)
      (setq result (strcat result ","))
    )
    (setq result (strcat result (item-to-json (nth idx items))))
    (setq idx (+ idx 1))
  )

  result
)

;;; Convert item to JSON object
(defun item-to-json (item / json-str idx pair)
  (setq json-str "{")
  (setq idx 0)

  (repeat (length item)
    (if (> idx 0)
      (setq json-str (strcat json-str ","))
    )
    (setq pair (nth idx item))
    (setq json-str (strcat json-str "\"" (car pair) "\":"))
    (setq json-str (strcat json-str (value-to-json (cadr pair))))
    (setq idx (+ idx 1))
  )

  (setq json-str (strcat json-str "}"))
  json-str
)

;;; Escape special characters for JSON strings
(defun json-escape (s)
  ;; IMPORTANT: Escape backslash FIRST, then other characters
  ;; Otherwise double-escaping creates invalid JSON sequences
  (setq s (vl-string-subst "\\\\" "\\" s))  ; \ → \\
  (setq s (vl-string-subst "\\\"" "\"" s))  ; " → \"
  (setq s (vl-string-subst "\\/" "/" s))    ; / → \/ (optional but safe)
  s
)

;;; Convert value to JSON
(defun value-to-json (val / num-result)
  (cond
    ((= val T) "true")
    ((null val) "null")
    (T
     (setq num-result (vl-catch-all-apply 'rtos (list val 2)))
     (if (vl-catch-all-error-p num-result)
       ;; rtos failed - val is not a number, treat as string
       (strcat "\"" (json-escape (strcat "" val)) "\"")
       ;; rtos succeeded - val is a number, return unquoted
       num-result
     )
    )
  )
)

;;; Write JSON to file
(defun write-json-file (json-str / file-path fp)
  (setq file-path (strcat (getenv "USERPROFILE") "\\Documents\\sitetrack_data.json"))

  (if (setq fp (open file-path "w"))
    (progn
      (write-line json-str fp)
      (close fp)
      (princ (strcat "\n>> Data also saved to: " file-path "\n"))
    )
  )
)

;;; Helper: write line to file
(defun write-line (str fp / )
  (write-string str fp)
  (write-string "\n" fp)
)

;;; Helper: write string to file
(defun write-string (str fp / )
  (if str
    (foreach char (vl-string->list str)
      (write-char char fp)
    )
  )
)

;;; Helper: write character to file
(defun write-char (char fp / )
  (princ (chr char) fp)
)

(princ "\n✓ GetData.lsp loaded successfully!")
(princ "\n  Run 'getData' command to extract selected objects.\n")
